// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
namespace APIView.DIff
{
    public struct InlineDiffLine<T>
    {
        public InlineDiffLine(T line, DiffLineKind kind)
        {
            Line = line;
            Kind = kind;
            OtherLine = default;
        }

        public InlineDiffLine(T lineA, T lineB, DiffLineKind kind)
        {
            Line = lineA;
            OtherLine = lineB;
            Kind = kind;
        }

        public T Line { get; }
        public T OtherLine { get; }
        public DiffLineKind Kind { get; }

        public override string ToString()
        {
            return Kind switch
            {
                DiffLineKind.Added => "+",
                DiffLineKind.Removed => "-",
                _ => " "
            } + Line;
        }
    }
}
