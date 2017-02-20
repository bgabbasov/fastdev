using System.Collections.Generic;
using FastDev.Web.Domain;

namespace FastDev.Web.Dto
{
    public class AListDto
    {
        public int TotalPages;
        public IList<A> Data;
    }
}
