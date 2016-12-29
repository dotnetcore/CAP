using System;

namespace Cap.Consistency.EventBus
{
    public class ReceiveResult
    {
        public bool IsSucceeded { get; set; }

        public bool IsVoid { get; set; }

        public object Result { get; set; }

        public Type ResultType { get; set; }

        public Exception Exception { get; set; }

        public ReceiveResult(bool isSucceeded, bool isVoid, object result, Exception ex = null, Type resultType = null) {
            this.IsSucceeded = isSucceeded;
            this.IsVoid = isVoid;
            this.Result = result;
            this.Exception = ex;
            this.ResultType = (resultType ?? result?.GetType()) ?? typeof(object);
        }
    }
}