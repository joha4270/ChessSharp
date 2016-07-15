using System;

namespace Framework
{
    public abstract class AbstractEngineOption
    {
        protected AbstractEngineOption(string name)
        {
            if (name.Contains("name"))
            {
                throw new ArgumentException(nameof(name) + " should not contain the string \"name\"");
            }

            Name = name;
        }

        public abstract string Present();

        public abstract string Public { get; set; }

        public string Name { get; }

    }

    public class BooleanEngineOption : AbstractEngineOption
    {
        

        public override string Public { get; set; }

        public override string Present()
        {
            return "option name " + Name + "type check default " + State;
        }


        public bool State { get; }

        public BooleanEngineOption(string name, bool defaultSetting) : base(name)
        {
            

            State = defaultSetting;
        }
    }
}
