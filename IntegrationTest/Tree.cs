using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationTest
{
    class Tree<T>
    {
        public Tree(T value)
        {
            Value = value;
        }

        public T Value { get; }
        public List<Tree<T>> Children { get; } = new List<Tree<T>>();

        private void Print(int depth, bool top, StringBuilder sb)
        {
            if (!top)
            {
                for (int i = 0; i < depth; i++)
                {
                    sb.Append(' ');
                }
            }

            sb.Append(Value);
            depth += Value.ToString().Length;
            if (Children.Count == 0)
            {
                sb.Append(Environment.NewLine);
            }
            else
            {
                sb.Append(" -> ");
                depth += 4;
                Children[0].Print(depth, true, sb);
                for (int i = 1; i < Children.Count; i++)
                {
                    Children[i].Print(depth, false, sb);
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            Print(0, true, sb);

            return sb.ToString();
        }
    }
}