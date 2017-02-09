using System;
using System.Collections;
using System.Collections.Generic;

namespace Cap.Consistency
{
    public class RouteTable : IReadOnlyList<QMessageMethodInfo>
    {

        public RouteTable() {

        }

        public RouteTable(List<QMessageMethodInfo> messageMethods) {
            QMessageMethods = messageMethods;
        }

        public QMessageMethodInfo this[int index] {
            get {
                throw new NotImplementedException();
            }
        }

        public int Count {
            get {
                throw new NotImplementedException();
            }
        }

        public List<QMessageMethodInfo> QMessageMethods { get; set; }

        public IEnumerator<QMessageMethodInfo> GetEnumerator() {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }
    }
}
