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

using System.Collections.Generic;

namespace SkyApm.Common
{
    public static class StackExtensions
    {
        public static bool TryPeek<T>(this Stack<T> stack, out T value)
        {
            if (stack == null || stack.Count == 0)
            {
                value = default(T);
                return false;
            }

            value = stack.Peek();
            return true;
        }

        public static bool TryPop<T>(this Stack<T> stack, out T value)
        {
            if (stack == null || stack.Count == 0)
            {
                value = default(T);
                return false;
            }

            value = stack.Pop();
            return true;
        }
    }
}