namespace Microsoft.Quantum.Diagnostics.Experimental {
    open Microsoft.Quantum.Preparation;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Diagnostics;

    // TODO: add location?
    operation DumpOperation(nQubits : Int, op : (Qubit[] => Unit)) : Unit {
        using ((reference, target) = (Qubit[nQubits], Qubit[nQubits])) {
            PrepareEntangledState(reference, target);
            op(target);
            DumpRegister((), reference + target);
            ResetAll(reference + target);
        }
    }

}
