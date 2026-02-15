using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Clawfoot.Result
{
    [AsyncMethodBuilder(typeof(ResultTaskMethodBuilder))]
    public class ResultTask
    {
        internal TaskCompletionSource<object> Tcs { get; } = new TaskCompletionSource<object>();
        
        public Result Result { get; set; }
        public Task Task { get; set; }

        public ResultTask()
        {
            this.Task = Task.CompletedTask;
        }

        public ResultTask(Task task)
        {
            this.Task = task;
        }
        
        public void CompleteWithResult(Result result)
        {
            this.Result = result;
            Tcs.SetResult(default); // Indicate completion without a result
        }

        public ResultTask Awaiter() => this;
        public bool IsCompleted => this.Task.IsCompleted;
        public void GetResult() => this.Task.GetAwaiter().GetResult();
        
        public static implicit operator ResultTask(Result result)
        {
            var resultTask = new ResultTask(Task.CompletedTask);
            resultTask.Result = result;
            return resultTask;
        }
    }
    
    public class ResultTaskMethodBuilder
    {
        private AsyncTaskMethodBuilder builder;
        public ResultTask ResultTask { get; set; }

        public static ResultTaskMethodBuilder Create()
        {
            var methodBuilder = new ResultTaskMethodBuilder { ResultTask = new ResultTask() };
            methodBuilder.builder = AsyncTaskMethodBuilder.Create();
            return methodBuilder;
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
            => builder.Start(ref stateMachine);

        public void SetStateMachine(IAsyncStateMachine stateMachine) => builder.SetStateMachine(stateMachine);

        public void SetResult()
        {
            // Assuming a default success result if not explicitly set
            ResultTask.Result ??= Result.Ok();
            ResultTask.Tcs.SetResult(null); // Signal completion
        }

        public void SetException(Exception exception) => builder.SetException(exception);

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) 
            where TAwaiter : INotifyCompletion 
            where TStateMachine : IAsyncStateMachine
            => builder.AwaitOnCompleted(ref awaiter, ref stateMachine);

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) 
            where TAwaiter : ICriticalNotifyCompletion 
            where TStateMachine : IAsyncStateMachine
            => builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
    }
    
    
    //
    //
    // [AsyncMethodBuilder(typeof(StatusTaskTaskMethodBuilder<>))]
    // public class StatusTask<T> : INotifyCompletion
    // {
    //     private Action _continuation;
    //
    //     public StatusTask()
    //     { }
    //
    //     public StatusTask(T value)
    //     {
    //         this.Value = new Status<T>(value);
    //         this.IsCompleted = true;
    //     }
    //
    //     public StatusTask(IError error)
    //     {
    //         this.Value = Status.Error<T>(error);
    //         this.IsCompleted = true;
    //     }
    //
    //     public StatusTask<T> GetAwaiter() => this;
    //
    //     public bool IsCompleted { get; private set; }
    //
    //     public Status<T> Value { get; private set; }
    //
    //     public Exception Exception { get; private set; }
    //
    //     public static StatusTask<T> AsError(IError error)
    //     {
    //         return new StatusTask<T>(error);
    //     }
    //
    //     public Status<T> GetResult()
    //     {
    //         if (!this.IsCompleted) throw new Exception("Not completed");
    //         if (this.Exception != null)
    //         {
    //             ExceptionDispatchInfo.Capture(this.Exception).Throw();
    //         }
    //         return this.Value;
    //     }
    //
    //
    //     internal void SetResult(IError error)
    //     {
    //         if (this.IsCompleted) throw new Exception("Already completed");
    //         this.Value = Status.Error<T>(error);
    //         this.IsCompleted = true;
    //         this._continuation?.Invoke();
    //     }
    //
    //     internal void SetResult(T value)
    //     {
    //         if (this.IsCompleted) throw new Exception("Already completed");
    //         this.Value = new Status<T>(value);
    //         this.IsCompleted = true;
    //         this._continuation?.Invoke();
    //     }
    //
    //     internal void SetException(Exception exception)
    //     {
    //         this.IsCompleted = true;
    //         this.Exception = exception;
    //     }
    //
    //     void INotifyCompletion.OnCompleted(Action continuation)
    //     {
    //         this._continuation = continuation;
    //         if (this.IsCompleted)
    //         {
    //             continuation();
    //         }
    //     }
    // }
    //
    //
    // public class StatusTaskTaskMethodBuilder<T>
    // {
    //     public StatusTaskTaskMethodBuilder()
    //         => this.Task = new StatusTask<T>();
    //
    //     public static StatusTaskTaskMethodBuilder<T> Create()
    //     => new StatusTaskTaskMethodBuilder<T>();
    //
    //     public void Start<TStateMachine>(ref TStateMachine stateMachine)
    //         where TStateMachine : IAsyncStateMachine
    //         => stateMachine.MoveNext();
    //
    //     public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    //
    //     public void SetException(Exception exception)
    //         => this.Task.SetException(exception);
    //
    //     public void SetResult(IError result)
    //         => this.Task.SetResult(result);
    //
    //     public void SetResult(T result)
    //         => this.Task.SetResult(result);
    //
    //     public void AwaitOnCompleted<TAwaiter, TStateMachine>(
    //         ref TAwaiter awaiter,
    //         ref TStateMachine stateMachine)
    //         where TAwaiter : INotifyCompletion
    //         where TStateMachine : IAsyncStateMachine
    //         => this.GenericAwaitOnCompleted(ref awaiter, ref stateMachine);
    //
    //     public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
    //         ref TAwaiter awaiter,
    //         ref TStateMachine stateMachine)
    //         where TAwaiter : ICriticalNotifyCompletion
    //         where TStateMachine : IAsyncStateMachine
    //         => this.GenericAwaitOnCompleted(ref awaiter, ref stateMachine);
    //
    //     public void GenericAwaitOnCompleted<TAwaiter, TStateMachine>(
    //         ref TAwaiter awaiter,
    //         ref TStateMachine stateMachine)
    //         where TAwaiter : INotifyCompletion
    //         where TStateMachine : IAsyncStateMachine
    //         => awaiter.OnCompleted(stateMachine.MoveNext);
    //
    //     public StatusTask<T> Task { get; }
    // }
    //
    // public sealed class StatusClassMethodBuilder
    // {
    //
    // }

}
