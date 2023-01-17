// This file is part of YamlDotNet - A .NET library for YAML.
// Copyright (c) Antoine Aubry and contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Globalization;
using YamlDotNet.Core;

namespace YamlDotNet.Serialization.ObjectGraphVisitors
{
    public sealed class AnchorAssigner : PreProcessingPhaseObjectGraphVisitorSkeleton, IAliasProvider
    {
        private class AnchorAssignment
        {
            public AnchorName Anchor = default;
            public int RefCount = 1;
            public AnchorAssignment()
            {

            }
            public AnchorAssignment(AnchorName name,int rc=1)
            {
                this.Anchor = name;
                this.RefCount = rc;
            }
            public override string ToString()
            {
                return this.Anchor.ToString() + ":" + RefCount;
            }
        }
        private uint nextId = 0;
        private Dictionary<object, AnchorAssignment> assignments = new Dictionary<object, AnchorAssignment>();
        private Dictionary<object, AnchorAssignment> results = new Dictionary<object, AnchorAssignment>();
        public AnchorAssigner(IEnumerable<IYamlTypeConverter> typeConverters)
            : base(typeConverters)
        {
        }
        //1.Enter
        //2.Visit
        //3.VisitMapping        
        protected override bool Enter(IObjectDescriptor value)
        {
            var bl = value.IsLink;
            value.IsLink = false;

            if (value.Value != null && bl)
            {
                if (!assignments.TryGetValue(value.Value, out var assignment))
                {
                    assignments.Add(value.Value, new AnchorAssignment()
                    {
                        Anchor = new AnchorName("o"
                            + (nextId++).ToString(CultureInfo.InvariantCulture))
                    });
                }
            }
            if (bl) return false;

            return true;
        }

        protected override bool EnterMapping(IObjectDescriptor key, IObjectDescriptor value)
        {
            return true;
        }

        protected override bool EnterMapping(IPropertyDescriptor key, IObjectDescriptor value)
        {
            return true;
        }

        protected override void VisitScalar(IObjectDescriptor scalar)
        {
            // Do not assign anchors to scalars
        }

        protected override void VisitMappingStart(IObjectDescriptor mapping, Type keyType, Type valueType)
        {
            VisitObject(mapping);
        }

        protected override void VisitMappingEnd(IObjectDescriptor mapping) { }

        protected override void VisitSequenceStart(IObjectDescriptor sequence, Type elementType)
        {
            VisitObject(sequence);
        }

        protected override void VisitSequenceEnd(IObjectDescriptor sequence) { }

        private void VisitObject(IObjectDescriptor value)
        {
        }
        public override void StartPreparation()
        {            
            this.nextId = 0;
            this.assignments.Clear();
            this.results.Clear();
        }

        public override void CompletePreparation()
        {
            this.results = new Dictionary<object, AnchorAssignment>(this.assignments);
            this.assignments.Clear();
        }
        AnchorName IAliasProvider.GetAlias(object target)
        {
            if (target != null && results.TryGetValue(target, out var r))
            {
                return r.Anchor;
            }
            return AnchorName.Empty;
        }

    }
}
