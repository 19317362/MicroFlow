using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MicroFlow
{
  public interface IResult
  {
    Guid SourceId { get; }
  }

  public sealed class Result<T> : IResult
  {
    private object myCurrentValue;

    public Result(Guid sourceId)
    {
      SourceId = sourceId;
      myCurrentValue = null;
      HasValue = false;
    }

    [CanBeNull]
    public T Get()
    {
      HasValue.AssertTrue("Value isn't set");

      return (T) myCurrentValue;
    }

    public bool HasValue { get; private set; }

    public Guid SourceId { get; }

    private event Action<T> OnResult;

    [NotNull]
    public static Result<T> Of<TActivity>([NotNull] ActivityNode<TActivity> node)
      where TActivity : class, IActivity<T>
    {
      node.AssertNotNull("node != null");

      var result = new Result<T>(node.Id);
      node.RegisterActivityTaskHandler(task => HandleCompletedTask(task, result));

      return result;
    }

    [NotNull]
    public static Result<T> Of<TActivity>([NotNull] ActivityDescriptor<TActivity> descriptor)
      where TActivity : class, IActivity<T>
    {
      descriptor.AssertNotNull("descriptor != null");

      var result = new Result<T>(descriptor.NotNull().Id);
      descriptor.RegisterActivityTaskHandler(task => HandleCompletedTask(task, result));

      return result;
    }

    internal void RegisterResultHandler([NotNull] Action<T> handler)
    {
      OnResult += handler.NotNull();
    }

    internal void UnregisterResultHandler([CanBeNull] Action<T> handler)
    {
      if (handler != null) OnResult -= handler;
    }

    private static void HandleCompletedTask(Task<object> task, Result<T> result)
    {
      if (task.Status == TaskStatus.RanToCompletion)
      {
        result.myCurrentValue = (T) task.Result;
        result.HasValue = true;
        result.ExecuteResultHandlers();
      }
      else
      {
        result.myCurrentValue = null;
        result.HasValue = false;
      }
    }

    private void ExecuteResultHandlers()
    {
      OnResult?.Invoke(Get());
    }
  }
}