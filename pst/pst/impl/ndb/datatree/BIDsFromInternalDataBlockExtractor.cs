﻿using pst.encodables.ndb;
using pst.encodables.ndb.blocks.data;
using pst.interfaces;
using pst.utilities;

namespace pst.impl.ndb.datatree
{
    class BIDsFromInternalDataBlockExtractor : IExtractor<InternalDataBlock, BID[]>
    {
        private readonly IDecoder<BID> bidDecoder;

        public BIDsFromInternalDataBlockExtractor(IDecoder<BID> bidDecoder)
        {
            this.bidDecoder = bidDecoder;
        }

        public BID[] Extract(InternalDataBlock parameter)
        {
            return bidDecoder.DecodeMultipleItems(parameter.NumberOfEntries, 8, parameter.Entries);
        }
    }
}
