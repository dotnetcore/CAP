/*
 * Licensed to the SkyAPM under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The SkyAPM licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */

using System;

namespace SkyApm.Tracing
{
    public class UniqueIdParser : IUniqueIdParser
    {
        public bool TryParse(string text, out UniqueId uniqueId)
        {
            uniqueId = default(UniqueId);
            if (string.IsNullOrEmpty(text)) return false;
#if SPAN
            var id = text.AsSpan();
            var index = FindIndex(id);

            if (index < 1) return false;
            var id1 = id.Slice(0, index);

            index = FindIndex(id.Slice(index + 1));
            if (index < 1) return false;

            if (!long.TryParse(id1, out var part0)) return false;
            if (!long.TryParse(id.Slice(id1.Length + 1, index), out var part1)) return false;
            if (!long.TryParse(id.Slice(id1.Length + index + 2), out var part2)) return false;
#else
            var parts = text.Split("\\.".ToCharArray(), 3);
            if (parts.Length < 3) return false;
            if (!long.TryParse(parts[0], out var part0)) return false;
            if (!long.TryParse(parts[1], out var part1)) return false;
            if (!long.TryParse(parts[2], out var part2)) return false;
#endif
            uniqueId = new UniqueId(part0, part1, part2);
            return true;
        }
#if SPAN
        private static int FindIndex(ReadOnlySpan<char> id)
        {
            var index = 0;
            do
            {
                if (id[index] == '\\' || id[index] == '.')
                    return index;
            } while (++index < id.Length);

            return -1;
        }
#endif
    }
}
