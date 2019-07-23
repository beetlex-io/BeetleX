namespace ServerX.Route
{
    public class ActionResult<T>
    {
        T _t;
        ActionResult(T t)
        {
            _t = t;
        }
        public static implicit operator ActionResult<T>(T obj)
        {
            return new ActionResult<T>(obj);
        }
        public T Result()
        {
            return _t;
        }
    }
}
