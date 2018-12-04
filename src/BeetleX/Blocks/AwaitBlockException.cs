using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Blocks
{
    public class AwaitBlockException<IDType> : Exception, IUniqueMessage<IDType>
    {
        public IDType _UniqueID { get; set; }


        public AwaitBlockException(IDType id)
        {
            _UniqueID = id;
        }
        public AwaitBlockException(IDType id, string message) : base(message)
        {
            _UniqueID = id;
        }

        public AwaitBlockException(IDType id, string message, Exception innerError) : base(message, innerError)
        {
            _UniqueID = id;
        }
    }
}
