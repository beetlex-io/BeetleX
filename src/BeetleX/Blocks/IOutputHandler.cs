using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Blocks
{
    public interface IUniqueMessagePipe<IDType>
    {
        Task<bool> Write(IUniqueMessage<IDType> item);

        AwaitBlock<IDType> AwaitBlock { get; set; }

        Type[] MessageTypes { get; }
    }
}
