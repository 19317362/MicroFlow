﻿namespace MicroFlow.Test
{
    public class Flow1 : Flow
    {
        private readonly IReader _reader;
        private readonly IWriter _writer;

        public Flow1(IReader reader, IWriter writer)
        {
            _reader = reader;
            _writer = writer;
        }

        public override string Name => "Flow1";

        protected override void Build(FlowBuilder builder)
        {
            var faultHandler = builder.FaultHandler<MyFaultHandler>("Global fault handler");
            var cancellationHandler = builder.Activity<MyCancellationHandler>("Global cancellation handler");

            builder.WithDefaultFaultHandler(faultHandler);
            builder.WithDefaultCancellationHandler(cancellationHandler);

            var inputFirst = builder.Activity<ReadIntActivity>("Read first number");
            var inputSecond = builder.Activity<ReadIntActivity>("Read second number");

            var first = Result<int>.Of(inputFirst);
            var second = Result<int>.Of(inputSecond);

            var condition = builder.Condition("Check whether a first number is greater than a second");
            condition.WithCondition(() => first.Get() > second.Get());

            var outputWhenTrue = builder.Activity<WriteMessageActivity>("Output when first > second");
            outputWhenTrue
                .Bind(x => x.Message)
                .To(() => $"{first.Get()} > {second.Get()}");

            var outputWhenFalse =
                builder.Activity<WriteMessageActivity>("Output when first <= second");
            outputWhenFalse
                .Bind(x => x.Message)
                .To(() => $"{first.Get()} <= {second.Get()}");

            builder.WithInitialNode(inputFirst);

            inputFirst.ConnectTo(inputSecond);
            inputSecond.ConnectTo(condition);

            condition.ConnectTrueTo(outputWhenTrue)
                     .ConnectFalseTo(outputWhenFalse);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IReader>(_reader);
            services.AddSingleton<IWriter>(_writer);
        }
    }
}