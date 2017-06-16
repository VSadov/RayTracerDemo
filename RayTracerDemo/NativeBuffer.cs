using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTracerDemo
{
    class NativeBuffer: OwnedNativeBuffer
    {
        public NativeBuffer(int length, IntPtr address): base(length, address){}

        protected override void Dispose(bool disposing) => GC.SuppressFinalize(this);
    }
}
