// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Bindables
{
    public class IndirectBindable<T> : Bindable<T>
    {
        private Func<T> getter;
        private Action<T> setter;

        public IndirectBindable(Func<T> getter, Action<T> setter, T defaultValue = default)
        {
            this.getter = getter;
            this.setter = setter;
            Default = defaultValue;
        }

        protected override T InternalValue { get => getter(); set => setter(value); }
    }
}
