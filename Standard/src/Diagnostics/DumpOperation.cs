#nullable enable
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;

using Microsoft.Quantum.Intrinsic;
using Microsoft.Quantum.Preparation;
using Microsoft.Quantum.Simulation;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;
using Microsoft.Quantum.Simulation.Simulators.Exceptions;
using Microsoft.Quantum.Standard.Emulation;
using NumSharp;
using static NumSharp.Slice;
using System.Numerics;

namespace Microsoft.Quantum.Diagnostics.Experimental
{
    public partial class DumpOperation
    {
        internal class ArrayDumper : QuantumSimulator.StateDumper
        {
            // NB: NumSharp does not yet support complex numbers, so we store data
            //     as an array with a trailing index of length 2.
            private NumSharp.NDArray? _data = null;
            public ArrayDumper(QuantumSimulator sim) : base(sim)
            {

            }

            public override bool Callback(uint idx, double real, double img)
            {
                if (_data as object == null) throw new Exception("Expected data buffer to be initialized before callback, but it was null.");
                _data[(int)idx, 0] = real;
                _data[(int)idx, 1] = img;
                return true;
            }

            public override bool Dump(IQArray<Qubit>? qubits = null)
            {
                var count = qubits == null
                            ? this.Simulator.QubitManager.GetAllocatedQubitsCount()
                            : qubits.Length;
                var nQubitsPerRegister = ((int) count / 2);
                _data = np.empty(new Shape(1 << ((int)count), 2));
                var result = base.Dump(qubits);

                // At this point, _data should be filled with the full state
                // vector, so let's display it, counting on the right display
                // encoder to be there to pack it into a table.
                var scaleFactor = System.Math.Sqrt(1 << nQubitsPerRegister);
                _data = scaleFactor * _data.reshape(1 << nQubitsPerRegister, 1 << nQubitsPerRegister, 2);
                System.Console.WriteLine("Real:");
                System.Console.WriteLine(_data[Ellipsis, 0].ToString());
                System.Console.WriteLine("Imag:");
                System.Console.WriteLine(_data[Ellipsis, 1].ToString());

                // Clean up the state vector buffer.
                _data = null;

                return result;
            }
        }

        /// <summary>
        ///  Provides a native emulation of the EstimateFrequency operation for adjointable operations when
        ///  the operation is executed using the full-state QuantumSimulator and the given
        ///  state preparation function does not contain any captured qubits via partial application.
        ///
        /// The way the emulation works is to invoke the state-preparation only once, and then look
        /// into the resulting QuantumSimulator's state to get the JointProbability and then
        /// use a classical binomial sampling to get a sample for the resulting probability.
        /// This is typically faster compared to run the state-preparation operation n-times and
        /// calculate the binomial estimation from it.
        /// </summary>
        public class Native : DumpOperation
        {
            private IOperationFactory Simulator { get; }

            protected Allocate Allocate { get; set; }
            protected Release Release { get; set; }
            protected ResetAll ResetAll { get; set; }
            protected PrepareEntangledState PrepareEntangledState { get; set; }

            public Native(IOperationFactory m) : base(m)
            {
                this.Simulator = m;
            }

            public override void Init()
            {
                base.Init();

                this.Allocate = this.Simulator.Get<Allocate>(typeof(Microsoft.Quantum.Intrinsic.Allocate));
                this.Release = this.Simulator.Get<Release>(typeof(Microsoft.Quantum.Intrinsic.Release));
                this.ResetAll = this.Simulator.Get<ResetAll>(typeof(Microsoft.Quantum.Intrinsic.ResetAll));
                this.PrepareEntangledState = this.Simulator.Get<PrepareEntangledState>(typeof(Microsoft.Quantum.Preparation.PrepareEntangledState));
            }

            private QVoid Dump(QuantumSimulator sim, long nQubits, ICallable op)
            {
                var arrayDumper = new ArrayDumper(sim);
                var reference = Allocate.Apply(nQubits);
                var target = Allocate.Apply(nQubits);

                PrepareEntangledState.Apply<QVoid>((reference, target));

                var combined = new QArray<Qubit>(reference.Concat(target));
                op.Apply<QVoid>(combined);

                arrayDumper.Dump();

                ResetAll.Apply(reference);
                ResetAll.Apply(target);
                Release.Apply(reference);
                Release.Apply(target);
                return QVoid.Instance;
            }

            private QVoid Dump(ToffoliSimulator sim, long nQubits, ICallable op)
            {
                return QVoid.Instance;
            }

            /// <summary>
            /// Overrides the body to do the emulation when possible. If emulation is not possible, then
            /// it just invokes the default Q# implementation.
            /// </summary>
            public override Func<(long, ICallable), QVoid> Body => (_args) =>
                Simulator switch {
                    QuantumSimulator qsim => this.Dump(qsim, _args.Item1, _args.Item2),
                    // ToffoliSimulator tsim => this.Dump(tsim, _args.Item1, _args.Item2),
                    _ => base.Body(_args)
                };
        }
    }
}
