using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Blocks
{
    public class AwaitBlock<IDType>
    {

        public const int TIMEOUT_PROCESS_ERROR = 0x0001;

        public const int PROCESS_MESSAGE_ERROR = 0x0002;

        public const int NOTFOUND_PROCESS_ERROR = 0x0003;

        public AwaitBlock()
        {
            mDispatchCenter = new Dispatchs.DispatchCenter<IUniqueMessage<IDType>>(OnProcess);
            mDispatchCenter.SetErrorHaneler(OnProcessError);
            awaiterManager = new AwaiterManager<IDType>();
            mTimer = new System.Threading.Timer(OnTimeOut, null, 1000, 1000);
        }

        public event System.EventHandler<EventAsyncMessageErrorArgs> Error;

        public event System.EventHandler<EventAwaiterNotFoundArgs> AwaiterNotFound;

        private Dispatchs.DispatchCenter<IUniqueMessage<IDType>> mDispatchCenter;

        private System.Threading.Timer mTimer;

        private AwaiterManager<IDType> awaiterManager;

        private Dictionary<Type, IUniqueMessagePipe<IDType>> mHandlers = new Dictionary<Type, IUniqueMessagePipe<IDType>>();

        public void ThrowBlockException(IUniqueMessage<IDType> id, string message, Exception innerError = null)
        {
            ThrowBlockException(id._UniqueID, message, innerError);
        }

        public void ThrowBlockException(IDType id, string message, Exception innerError = null)
        {
            AwaitBlockException<IDType> error = new AwaitBlockException<IDType>(id, message, innerError);
            Enqueue(error);
        }

        private void OnTimeOut(object state)
        {
            mTimer.Change(-1, -1);
            try
            {
                for (int i = 0; i < awaiterManager.AwaiterTables.Count; i++)
                {
                    var items = awaiterManager.AwaiterTables[i].TimeOut(TimeWatch.GetElapsedMilliseconds());
                    if (items.Count > 0)
                        for (int k = 0; k < items.Count; k++)
                        {
                            ThrowBlockException(items[k].ID, $"{items[k].ID} process timeout!");
                        }
                }
            }
            catch (Exception e_)
            {
                OnError(null, TIMEOUT_PROCESS_ERROR, "TimeOut handler error", e_);
            }
            finally
            {
                mTimer.Change(1000, 1000);
            }
        }

        private IAwaitMessage<IDType> GetMessageAwaiter(IDType id)
        {
            return awaiterManager.Get(id);
        }

        private void AddMessageAwaiter(IAwaitMessage<IDType> item)
        {
            awaiterManager.Add(item);
        }

        protected virtual void OnProcess(IUniqueMessage<IDType> item)
        {
            try
            {
                var msg = GetMessageAwaiter(item._UniqueID);
                if (msg == null)
                {
                    OnNotfound(item);
                }
                else
                {
                    if (item is Exception error)
                        msg.Error(error);
                    else
                        msg.Success(item);
                }
            }
            catch (Exception e_)
            {
                OnProcessError(item, e_);
            }
        }

        public void Enqueue(IUniqueMessage<IDType> item)
        {
            mDispatchCenter.Enqueue(item, 3);
        }

        public Task<T> Request<T>(IUniqueMessage<IDType> item, IUniqueMessagePipe<IDType> messagePipe, int timeout = 10000)
        {
            AwaitMessage<IDType, T> result = new AwaitMessage<IDType, T>(item._UniqueID);
            result.TimeOut = TimeWatch.GetElapsedMilliseconds() + timeout;
            AddMessageAwaiter(result);
            messagePipe.AwaitBlock = this;
            try
            {
                messagePipe.Write(item);
            }
            catch (Exception e_)
            {
                ThrowBlockException(item, e_.Message, e_);
            }
            return result.Task;
        }

        private void OnProcessError(IUniqueMessage<IDType> item, Exception error)
        {
            OnError(item, PROCESS_MESSAGE_ERROR, $"process {item._UniqueID} message error!", error);
        }

        protected virtual void OnNotfound(IUniqueMessage<IDType> item)
        {
            try
            {
                if (AwaiterNotFound != null)
                {
                    EventAwaiterNotFoundArgs e = new EventAwaiterNotFoundArgs();
                    e.Source = item;
                    AwaiterNotFound?.Invoke(this, e);
                }
            }
            catch (Exception e_)
            {
                OnError(item, NOTFOUND_PROCESS_ERROR, $"process {item._UniqueID} message notfound event error!", e_);
            }
        }

        protected virtual void OnError(object source, int code, string message, Exception error)
        {
            try
            {
                if (Error != null)
                {
                    EventAsyncMessageErrorArgs e = new EventAsyncMessageErrorArgs();
                    e.Source = source;
                    e.Error = error;
                    e.Code = code;
                    e.Message = message;
                    Error(this, e);
                }
            }
            catch
            {

            }

        }

    }








}
