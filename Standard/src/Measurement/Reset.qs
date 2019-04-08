// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Measurement {
    open Microsoft.Quantum.Primitive;
    open Microsoft.Quantum.Canon;

    operation _BasisChangeZtoY (target : Qubit) : Unit {
        body (...) {
            H(target);
            S(target);
        }

        adjoint invert;
        controlled distribute;
        controlled adjoint distribute;
    }

    /// # Summary
    /// Measures a single qubit in the `Z` basis,
    /// and resets it to the standard basis state
    /// |0〉 following the measurement.
    ///
    /// # Input
    /// ## target
    /// A single qubit to be measured.
    ///
    /// # Output
    /// The result of measuring `target` in the Pauli $Z$ basis.
    operation MResetZ (target : Qubit) : Result
    {
        let result = M(target);
        
        if (result == One)
        {
            // Recall that the +1 eigenspace of a measurement operator corresponds to
            // the Result case Zero. Thus, if we see a One case, we must reset the state
            // have +1 eigenvalue.
            X(target);
        }
        
        return result;
    }
    
    
    /// # Summary
    /// Measures a single qubit in the X basis,
    /// and resets it to the standard basis state
    /// |0〉 following the measurement.
    ///
    /// # Input
    /// ## target
    /// A single qubit to be measured.
    ///
    /// # Output
    /// The result of measuring `target` in the Pauli $X$ basis.
    operation MResetX (target : Qubit) : Result
    {
        let result = Measure([PauliX], [target]);
        
        // We must return the qubit to the Z basis as well.
        H(target);
        
        if (result == One)
        {
            // Recall that the +1 eigenspace of a measurement operator corresponds to
            // the Result case Zero. Thus, if we see a One case, we must reset the state
            // have +1 eigenvalue.
            X(target);
        }
        
        return result;
    }
    
    
    /// # Summary
    /// Measures a single qubit in the Y basis,
    /// and resets it to the standard basis state
    /// |0〉 following the measurement.
    ///
    /// # Input
    /// ## target
    /// A single qubit to be measured.
    ///
    /// # Output
    /// The result of measuring `target` in the Pauli $Y$ basis.
    operation MResetY (target : Qubit) : Result
    {
        let result = Measure([PauliY], [target]);
        
        // We must return the qubit to the Z basis as well.
        Adjoint _BasisChangeZtoY(target);
        
        if (result == One)
        {
            // Recall that the +1 eigenspace of a measurement operator corresponds to
            // the Result case Zero. Thus, if we see a One case, we must reset the state
            // have +1 eigenvalue.
            X(target);
        }
        
        return result;
    }

}