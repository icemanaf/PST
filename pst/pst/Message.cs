﻿using pst.core;
using pst.encodables;
using pst.encodables.ndb;
using pst.interfaces;
using pst.interfaces.ltp;
using pst.interfaces.ltp.pc;
using pst.interfaces.ltp.tc;
using pst.interfaces.ndb;
using pst.utilities;
using System.Linq;

namespace pst
{
    public class Message
    {
        private readonly BID nodeBlockId;
        private readonly BID subnodeBlockId;
        private readonly IDecoder<NID> nidDecoder;
        private readonly ITCReader<NID> nidBasedTableContextReader;
        private readonly ITCReader<Tag> tagBasedTableContextReader;
        private readonly ISubNodesEnumerator subnodesEnumerator;
        private readonly IPropertyNameToIdMap propertyNameToIdMap;
        private readonly IPCBasedPropertyReader pcBasedPropertyReader;

        internal Message(
            BID nodeBlockId,
            BID subnodeBlockId,
            IDecoder<NID> nidDecoder,
            ITCReader<NID> nidBasedTableContextReader,
            ITCReader<Tag> tagBasedTableContextReader,
            ISubNodesEnumerator subnodesEnumerator,
            IPropertyNameToIdMap propertyNameToIdMap,
            IPCBasedPropertyReader pcBasedPropertyReader)
        {
            this.nodeBlockId = nodeBlockId;
            this.subnodeBlockId = subnodeBlockId;

            this.nidDecoder = nidDecoder;
            this.subnodesEnumerator = subnodesEnumerator;
            this.propertyNameToIdMap = propertyNameToIdMap;
            this.pcBasedPropertyReader = pcBasedPropertyReader;
            this.nidBasedTableContextReader = nidBasedTableContextReader;
            this.tagBasedTableContextReader = tagBasedTableContextReader;
        }

        public Recipient[] GetRecipients()
        {
            var subnodes =
                subnodesEnumerator.Enumerate(subnodeBlockId);

            var recipientTableEntry =
                subnodes.First(s => s.LocalSubnodeId.Type == Globals.NID_TYPE_RECIPIENT_TABLE);

            var rows =
                nidBasedTableContextReader.GetAllRows(
                    recipientTableEntry.DataBlockId,
                    recipientTableEntry.SubnodeBlockId);

            return
                rows
                .Select(
                    r =>
                    {
                        return
                            new Recipient(
                                recipientTableEntry.DataBlockId,
                                recipientTableEntry.SubnodeBlockId,
                                Tag.OfValue(r.RowId),
                                propertyNameToIdMap, 
                                tagBasedTableContextReader);
                    })
                .ToArray();
        }

        public Attachment[] GetAttachments()
        {
            var subnodes =
                subnodesEnumerator.Enumerate(subnodeBlockId);

            var attachmentsTableEntry =
                subnodes.First(s => s.LocalSubnodeId.Type == Globals.NID_TYPE_ATTACHMENT_TABLE);

            var rowsIds =
                nidBasedTableContextReader.GetAllRowIds(attachmentsTableEntry.DataBlockId);

            return
                rowsIds
                .Select(
                    id =>
                    {
                        var attachmentNodeId = nidDecoder.Decode(id.RowId);

                        var attachmentSubnodeEntry =
                            subnodes.First(s => s.LocalSubnodeId.Value == attachmentNodeId.Value);

                        return
                            new Attachment(
                                attachmentSubnodeEntry.DataBlockId,
                                attachmentSubnodeEntry.SubnodeBlockId,
                                nidDecoder,
                                nidBasedTableContextReader,
                                tagBasedTableContextReader,
                                subnodesEnumerator,
                                propertyNameToIdMap, 
                                pcBasedPropertyReader);
                    })
                .ToArray();
        }

        public Maybe<PropertyValue> GetProperty(NumericalPropertyTag propertyTag)
        {
            var propertyId = propertyNameToIdMap.GetPropertyId(propertyTag.Set, propertyTag.Id);

            if (propertyId.HasNoValue)
            {
                return Maybe<PropertyValue>.NoValue();
            }

            return GetProperty(new PropertyTag(propertyId.Value, propertyTag.Type));
        }

        public Maybe<PropertyValue> GetProperty(StringPropertyTag propertyTag)
        {
            var propertyId = propertyNameToIdMap.GetPropertyId(propertyTag.Set, propertyTag.Name);

            if (propertyId.HasNoValue)
            {
                return Maybe<PropertyValue>.NoValue();
            }

            return GetProperty(new PropertyTag(propertyId.Value, propertyTag.Type));
        }

        public Maybe<PropertyValue> GetProperty(PropertyTag propertyTag)
        {
            return pcBasedPropertyReader.ReadProperty(nodeBlockId, subnodeBlockId, propertyTag);
        }
    }
}