﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace MonoGame.Ruge.Glide {
    public class Tweener : Tween.TweenerImpl {}

    public partial class Tween {
        
        //	lol get it
        private interface IRemoveTweens {
            void Remove(Tween t);
        }

        public class TweenerImpl : IRemoveTweens {

            private static readonly Dictionary<Type, ConstructorInfo> registeredLerpers;
            private readonly List<Tween> toRemove;
            private readonly List<Tween> toAdd;
            private readonly List<Tween> allTweens;
            private readonly Dictionary<object, List<Tween>> tweens;

            static TweenerImpl() {
                registeredLerpers = new Dictionary<Type, ConstructorInfo>();
                var numericTypes = new[] {
                    typeof(short),
                    typeof(int),
                    typeof(long),
                    typeof(ushort),
                    typeof(uint),
                    typeof(ulong),
                    typeof(float),
                    typeof(double)
                };

                for (var i = 0; i < numericTypes.Length; i++)
                    SetLerper<NumericLerper>(numericTypes[i]);
            }

            protected TweenerImpl() {
                tweens = new Dictionary<object, List<Tween>>();
                toRemove = new List<Tween>();
                toAdd = new List<Tween>();
                allTweens = new List<Tween>();
            }

            void IRemoveTweens.Remove(Tween tween) {
                toRemove.Add(tween);
            }

            /// <summary>
            ///     Associate a Lerper class with a property type.
            /// </summary>
            /// <typeparam name="TLerper">The Lerper class to use for properties of the given type.</typeparam>
            /// <param name="propertyType">The type of the property to associate the given Lerper with.</param>
            public static void SetLerper<TLerper>(Type propertyType) where TLerper : Lerper, new() {
                registeredLerpers[propertyType] = typeof(TLerper).GetConstructor(Type.EmptyTypes);
            }

            /// <summary>
            ///     <para>Tweens a set of properties on an object.</para>
            ///     <para>To tween instance properties/fields, pass the object.</para>
            ///     <para>To tween static properties/fields, pass the type of the object, using typeof(ObjectType) or object.GetType().</para>
            /// </summary>
            /// <param name="target">The object or type to tween.</param>
            /// <param name="values">The values to tween to, in an anonymous type ( new { prop1 = 100, prop2 = 0} ).</param>
            /// <param name="duration">Duration of the tween in seconds.</param>
            /// <param name="delay">Delay before the tween starts, in seconds.</param>
            /// <param name="overwrite">Whether pre-existing tweens should be overwritten if this tween involves the same properties.</param>
            /// <returns>The tween created, for setting properties on.</returns>
            public Tween Tween<T>(T target, object values, float duration, float delay = 0, bool overwrite = true)
                where T : class {
                if (target == null)
                    throw new ArgumentNullException("target");

                //	Prevent tweening on structs if you cheat by casting target as Object
                var targetType = target.GetType();
                if (targetType.IsValueType)
                    throw new Exception("Target of tween cannot be a struct!");

                var tween = new Tween(target, duration, delay, this);
                AddAndRemove();
                toAdd.Add(tween);

                if (values == null) // valid in case of manual timer
                    return tween;

                var props = values.GetType().GetProperties();
                for (var i = 0; i < props.Length; ++i) {
                    List<Tween> library = null;
                    if (overwrite && tweens.TryGetValue(target, out library)) {
                        for (var j = 0; j < library.Count; j++)
                            library[j].Cancel(props[i].Name);
                    }

                    var property = props[i];
                    var info = new GlideInfo(target, property.Name);
                    var to = new GlideInfo(values, property.Name, false);
                    var lerper = CreateLerper(info.PropertyType);

                    tween.AddLerp(lerper, info, info.Value, to.Value);
                }

                return tween;
            }

            /// <summary>
            ///     Starts a simple timer for setting up callback scheduling.
            /// </summary>
            /// <param name="duration">How long the timer will run for, in seconds.</param>
            /// <param name="delay">How long to wait before starting the timer, in seconds.</param>
            /// <returns>The tween created, for setting properties.</returns>
            public Tween Timer(float duration, float delay = 0) {
                var tween = new Tween(null, duration, delay, this);
                AddAndRemove();
                toAdd.Add(tween);
                return tween;
            }

            /// <summary>
            ///     Remove tweens from the tweener without calling their complete functions.
            /// </summary>
            public void Cancel() {
                toRemove.AddRange(allTweens);
            }

            /// <summary>
            ///     Assign tweens their final value and remove them from the tweener.
            /// </summary>
            public void CancelAndComplete() {
                for (var i = 0; i < allTweens.Count; ++i)
                    allTweens[i].CancelAndComplete();
            }

            /// <summary>
            ///     Set tweens to pause. They won't update and their delays won't tick down.
            /// </summary>
            public void Pause() {
                for (var i = 0; i < allTweens.Count; ++i) {
                    var tween = allTweens[i];
                    tween.Pause();
                }
            }

            /// <summary>
            ///     Toggle tweens' paused value.
            /// </summary>
            public void PauseToggle() {
                for (var i = 0; i < allTweens.Count; ++i) {
                    var tween = allTweens[i];
                    tween.PauseToggle();
                }
            }

            /// <summary>
            ///     Resumes tweens from a paused state.
            /// </summary>
            public void Resume() {
                for (var i = 0; i < allTweens.Count; ++i) {
                    var tween = allTweens[i];
                    tween.Resume();
                }
            }

            /// <summary>
            ///     Updates the tweener and all objects it contains.
            /// </summary>
            /// <param name="secondsElapsed">Seconds elapsed since last update.</param>
            public void Update(float secondsElapsed) {
                for (var i = 0; i < allTweens.Count; ++i)
                    allTweens[i].Update(secondsElapsed);

                AddAndRemove();
            }

            private Lerper CreateLerper(Type propertyType) {
                ConstructorInfo lerper = null;
                if (!registeredLerpers.TryGetValue(propertyType, out lerper))
                    throw new Exception(string.Format("No Lerper found for type {0}.", propertyType.FullName));

                return (Lerper) lerper.Invoke(null);
            }

            private void AddAndRemove() {
                for (var i = 0; i < toAdd.Count; ++i) {
                    var tween = toAdd[i];
                    allTweens.Add(tween);
                    if (tween.Target == null) continue; //	don't sort timers by target

                    List<Tween> list = null;
                    if (!tweens.TryGetValue(tween.Target, out list))
                        tweens[tween.Target] = list = new List<Tween>();

                    list.Add(tween);
                }

                for (var i = 0; i < toRemove.Count; ++i) {
                    var tween = toRemove[i];
                    allTweens.Remove(tween);
                    if (tween.Target == null) continue; // see above

                    List<Tween> list = null;
                    if (tweens.TryGetValue(tween.Target, out list)) {
                        list.Remove(tween);
                        if (list.Count == 0) {
                            tweens.Remove(tween.Target);
                        }
                    }

                    allTweens.Remove(tween);
                }

                toAdd.Clear();
                toRemove.Clear();
            }

            private class NumericLerper : Lerper {
                private float from;
                private float to;
                private float range;

                public override void Initialize(object fromValue, object toValue, Behavior behavior) {
                    from = Convert.ToSingle(fromValue);
                    to = Convert.ToSingle(toValue);
                    range = to - from;

                    if (behavior.HasFlag(Behavior.Rotation)) {
                        var angle = from;
                        if (behavior.HasFlag(Behavior.RotationRadians))
                            angle *= DEG;

                        if (angle < 0)
                            angle = 360 + angle;

                        var r = angle + range;
                        var d = r - angle;
                        var a = Math.Abs(d);

                        if (a >= 180) range = (360 - a) * (d > 0 ? -1 : 1);
                        else range = d;
                    }
                }

                public override object Interpolate(float t, object current, Behavior behavior) {
                    var value = from + range * t;
                    if (behavior.HasFlag(Behavior.Rotation)) {
                        if (behavior.HasFlag(Behavior.RotationRadians))
                            value *= DEG;

                        value %= 360.0f;

                        if (value < 0)
                            value += 360.0f;

                        if (behavior.HasFlag(Behavior.RotationRadians))
                            value *= RAD;
                    }

                    if (behavior.HasFlag(Behavior.Round)) value = (float) Math.Round(value);

                    var type = current.GetType();
                    return Convert.ChangeType(value, type);
                }
            }

            #region Target control

            /// <summary>
            ///     Cancel all tweens with the given target.
            /// </summary>
            /// <param name="target">The object being tweened that you want to cancel.</param>
            public void TargetCancel(object target) {
                List<Tween> list;
                if (tweens.TryGetValue(target, out list)) {
                    for (var i = 0; i < list.Count; ++i)
                        list[i].Cancel();
                }
            }

            /// <summary>
            ///     Cancel tweening named properties on the given target.
            /// </summary>
            /// <param name="target">The object being tweened that you want to cancel properties on.</param>
            /// <param name="properties">The properties to cancel.</param>
            public void TargetCancel(object target, params string[] properties) {
                List<Tween> list;
                if (tweens.TryGetValue(target, out list)) {
                    for (var i = 0; i < list.Count; ++i)
                        list[i].Cancel(properties);
                }
            }

            /// <summary>
            ///     Cancel, complete, and call complete callbacks for all tweens with the given target..
            /// </summary>
            /// <param name="target">The object being tweened that you want to cancel and complete.</param>
            public void TargetCancelAndComplete(object target) {
                List<Tween> list;
                if (tweens.TryGetValue(target, out list)) {
                    for (var i = 0; i < list.Count; ++i)
                        list[i].CancelAndComplete();
                }
            }


            /// <summary>
            ///     Pause all tweens with the given target.
            /// </summary>
            /// <param name="target">The object being tweened that you want to pause.</param>
            public void TargetPause(object target) {
                List<Tween> list;
                if (tweens.TryGetValue(target, out list)) {
                    for (var i = 0; i < list.Count; ++i)
                        list[i].Pause();
                }
            }

            /// <summary>
            ///     Toggle the pause state of all tweens with the given target.
            /// </summary>
            /// <param name="target">The object being tweened that you want to toggle pause.</param>
            public void TargetPauseToggle(object target) {
                List<Tween> list;
                if (tweens.TryGetValue(target, out list)) {
                    for (var i = 0; i < list.Count; ++i)
                        list[i].PauseToggle();
                }
            }

            /// <summary>
            ///     Resume all tweens with the given target.
            /// </summary>
            /// <param name="target">The object being tweened that you want to resume.</param>
            public void TargetResume(object target) {
                List<Tween> list;
                if (tweens.TryGetValue(target, out list)) {
                    for (var i = 0; i < list.Count; ++i)
                        list[i].Resume();
                }
            }

            #endregion
        }
    }
}