//
//  JIT26Breakpoint.swift
//  MeloNX
//
//  Created by Stossy11 on 25/4/2026.
//

import Foundation

// `SWIFT_DEFAULT_ACTOR_ISOLATION = MainActor` makes every unannotated decl in this module
// @MainActor, including this one -- and a @MainActor function converted to @convention(c)
// is both wrong for a signal handler and makes SILGen emit an isolation thunk. Opt out.
nonisolated func handler(sig: Int32, info: UnsafeMutablePointer<siginfo_t>?, context: UnsafeMutableRawPointer?) {
    guard let context = context else { return }
    let uc = context.bindMemory(to: ucontext_t.self, capacity: 1)
    uc.pointee.uc_mcontext.pointee.__ss.__pc += 4
    uc.pointee.uc_mcontext.pointee.__ss.__x.0 = 0
}

// here to stop app from crashing when app launched without JIT attached on 26 TXM
func JIT26BreakpointHandler() {
    var sa = sigaction()
    sa.sa_flags = SA_SIGINFO

    sa.__sigaction_u.__sa_sigaction = handler

    sigaction(SIGTRAP, &sa, nil)
    sigaction(SIGBUS, &sa, nil)
}

