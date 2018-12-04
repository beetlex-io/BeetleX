using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Blocks
{
    public interface IUniqueMessage<T>
    {
        T _UniqueID { get; set; }
    }
}
