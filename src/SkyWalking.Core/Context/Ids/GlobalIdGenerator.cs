/*
 * Licensed to the OpenSkywalking under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
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
using System.Threading;
using SkyWalking.Config;
using SkyWalking.Dictionarys;

namespace SkyWalking.Context.Ids
{
    public static class GlobalIdGenerator
    {
        private static readonly ThreadLocal<IDContext> threadIdSequence = new ThreadLocal<IDContext>(() => new IDContext(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 0));

        public static ID Generate()
        {
            if (RemoteDownstreamConfig.Agent.ApplicationId == DictionaryUtil.NullValue)
            {
                throw new InvalidOperationException();
            }

            IDContext context = threadIdSequence.Value;

            return new ID(
                RemoteDownstreamConfig.Agent.ApplicationInstanceId,
                Thread.CurrentThread.ManagedThreadId,
                context.NextSeq()
            );
        }

        private class IDContext
        {
            private long _lastTimestamp;
            private short _threadSeq;

            // Just for considering time-shift-back only.
            private long _runRandomTimestamp;
            private int _lastRandomValue;
            private readonly Random _random;

            public IDContext(long lastTimestamp, short threadSeq)
            {
                _lastTimestamp = lastTimestamp;
                _threadSeq = threadSeq;
                _random = new Random();
            }

            public long NextSeq()
            {
                return GetTimestamp() * 10000 + NextThreadSeq();
            }

            private long GetTimestamp()
            {
                long currentTimeMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                if (currentTimeMillis < _lastTimestamp)
                {
                    // Just for considering time-shift-back by Ops or OS. @hanahmily 's suggestion.
                    if (_runRandomTimestamp != currentTimeMillis)
                    {
                        _lastRandomValue = _random.Next();
                        _runRandomTimestamp = currentTimeMillis;
                    }
                    return _lastRandomValue;
                }
                else
                {
                    _lastTimestamp = currentTimeMillis;
                    return _lastTimestamp;
                }
            }

            private short NextThreadSeq()
            {
                if (_threadSeq == 10000)
                {
                    _threadSeq = 0;
                }
                return _threadSeq++;
            }
        }
    }
}
