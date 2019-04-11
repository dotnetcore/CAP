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
    public struct UniqueId : IEquatable<UniqueId>
    {
        public long Part1 { get; }

        public long Part2 { get; }

        public long Part3 { get; }

        public UniqueId(long part1, long part2, long part3)
        {
            Part1 = part1;
            Part2 = part2;
            Part3 = part3;
        }

        public override string ToString() => $"{Part1}.{Part2}.{Part3}";

        public bool Equals(UniqueId other) =>
            Part1 == other.Part1 && Part2 == other.Part2 && Part3 == other.Part3;

        public override bool Equals(object obj) =>
            obj is UniqueId other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Part1.GetHashCode();
                hashCode = (hashCode * 397) ^ Part2.GetHashCode();
                hashCode = (hashCode * 397) ^ Part3.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(UniqueId left, UniqueId right) => left.Equals(right);

        public static bool operator !=(UniqueId left, UniqueId right) => !left.Equals(right);
    }
}
