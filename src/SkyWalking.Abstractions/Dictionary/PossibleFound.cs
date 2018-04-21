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

namespace SkyWalking.Dictionarys
{
    public abstract class PossibleFound
    {
        private bool _found;
        private int _value;

        protected PossibleFound(int value)
        {
            _found = true;
            _value = value;
        }

        protected PossibleFound()
        {
            _found = false;
        }

        public virtual void InCondition(Action<int> foundCondition, Action notFoundCondition)
        {
            if (_found)
            {
                foundCondition?.Invoke(_value);
            }
            else
            {
                notFoundCondition?.Invoke();
            }
        }

        public virtual object InCondition(Func<int, object> foundCondition, Func<object> notFoundCondition)
        {
            return _found ? foundCondition?.Invoke(_value) : notFoundCondition?.Invoke();
        }
    }
}