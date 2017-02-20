using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FastDev.Web.Domain
{
    public class A
    {
        public A()
        {
            
        }
        public A(Guid id, string file1, string file2, string file3)
        {
            Id = id;
            File1 = file1;
            File2 = file2;
            File3 = file3;
        }

        public Guid Id { get; set; }
        public string File1 { get; set; }
        public string File2 { get; set; }
        public string File3 { get; set; }
    }
}
