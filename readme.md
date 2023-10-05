# joby

Joby is a simple utility for scheduling recurring tasks in your application without having to worry about settings things the right way. These jobs run on separate threads that already have all the necessary properties to have your background task running.

See how complicated and hard it is to create a Job with joby:

```cs
class HelloJob : Job
{
    public override TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(1);

    public override void Run()
    {
        Console.WriteLine("Hello, world");
    }
}
```

And to start it:

```cs
static void Main(string[] args)
{
    Job.Start<HelloJob>();
    // or
    new HelloJob().Start();
}
```

## Usage

All configuration is made within your job class itself. There you can configure the execution interval, delay, initialization function, termination function, error callback, etc.

The configuration is self-explanatory, but here we will show examples and details of how each function works:

### Creating a job

The `Job` class is an abstract object that represents your job. It requires two members: `Interval` and `Run()`. Run is the callback that runs every time your job reaches the desired interval.

```cs
class MyJob : Job
{
    public override TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(1);

    public override void Run()
    {
        ; // do things
    }
}
```

Interval represents the time between one Run() execution and the next. However, this time is synchronous with each execution. Then, the next execution waits for the previous one to finish before it can be started, even if the execution time is greater than Interval.

Therefore, if you have a 10 second interval between each Run() and your method takes an average of 5 seconds to execute, you will have a task running every 15 seconds.

If you want them to be executed immediately, even simultaneously, you can set your `Run()` to be an async method:

```cs
public override async void Run()
{
    ; // do your async things
}
```

### Error handling

Some jobs may throw errors, but don't worry, you have them at hand, and you don't need to create try...catch for them.

```cs
class MyJob : Job
{
    public override TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(1);

    public override void OnException(Exception ex, JobEventContext context)
    {
        // handle ex here
        base.OnException(ex, context);
    }

    public override void Run()
    {
        int x = 0;
        int y = 250 / x;
    }
}
```

Inside `context` at `OnException` you can get where your exception was thrown, whether it was in `Run()`, `Setup()` or `Terminate()`.

You can also define what happens to the job after an error occurs:

```cs
public override JobExceptionHandler ExceptionHandler { get; set; }
    = JobExceptionHandler.Continue; // or Stop
```

### Setup, delay and terminate your job

Before starting execution, `Setup()` is called immediately. And after the execution ends with Stop(), the `Terminate()` method is called immediately.

```cs
public override void Setup()
{
    // setup your job
}

public override void Terminate()
{
    // your job was stopped
}
```

You can also delay the first run of your job:

```cs
public override TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(10);
```

### Start, stop or restart your job

Your job comes standard with the methods:

```cs
var job = new MyJob();
job.Start();
job.Restart();
job.Stop();
```

Or, if you're using `Job.Start<>`, you can stop all running jobs at once with:

```cs
Job.StopAll();
```

### Enable or disable a job

By marking your job as "Enabled" to False, it will no longer start.

> Warning: when a job tries to start with `Enabled = false`, an exception is thrown at application scope.

```cs
myJob.Enabled = false;
```