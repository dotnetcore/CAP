using System;
using System.Collections;
using System.Collections.Generic;
using Cap.Consistency.Abstractions;

namespace Cap.Consistency
{
    public class RouteTable : IReadOnlyList<ConsumerExecutorDescriptor>
    {

        public RouteTable() {

        }

        public RouteTable(List<ConsumerExecutorDescriptor> messageMethods) {
            QMessageMethods = messageMethods;
        }

        public ConsumerExecutorDescriptor this[int index] {
            get {
                throw new NotImplementedException();
            }
        }

        public int Count {
            get {
                throw new NotImplementedException();
            }
        }

        public List<ConsumerExecutorDescriptor> QMessageMethods { get; set; }

        public IEnumerator<ConsumerExecutorDescriptor> GetEnumerator() {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }
    }
}
