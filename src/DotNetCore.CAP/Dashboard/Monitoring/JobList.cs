using System.Collections.Generic;

namespace DotNetCore.CAP.Dashboard.Monitoring
{
    public class JobList<TDto> : List<KeyValuePair<string, TDto>>
    {
        public JobList(IEnumerable<KeyValuePair<string, TDto>> source)
            : base(source)
        {
        }
    }
}