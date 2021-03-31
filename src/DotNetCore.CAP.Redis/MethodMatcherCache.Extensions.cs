using DotNetCore.CAP.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Redis
{
    static class MethodMatcherCacheExtensions
    {
        public static string GetGroupByTopic(this MethodMatcherCache source, string topicName)
        {
            var groupsMap = source.GetCandidatesMethodsOfGroupNameGrouped();

            return (from groupMap in groupsMap
                    from topic in groupMap.Value
                    where topic.TopicName == topicName
                    select topic.Attribute.Group).FirstOrDefault();
        }
    }
}
