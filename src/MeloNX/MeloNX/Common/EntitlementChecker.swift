//
//  EntitlementChecker.swift
//  MeloNX
//
//  Created by Stossy11 on 15/02/2025.
//

import Foundation
import Security

typealias SecTaskRef = OpaquePointer

@_silgen_name("SecTaskCopyValueForEntitlement")
func SecTaskCopyValueForEntitlement(
    _ task: SecTaskRef,
    _ entitlement: NSString,
    _ error: NSErrorPointer
) -> CFTypeRef?

@_silgen_name("SecTaskCopyTeamIdentifier")
func SecTaskCopyTeamIdentifier(
    _ task: SecTaskRef,
    _ error: NSErrorPointer
) -> NSString?

@_silgen_name("SecTaskCreateFromSelf")
func SecTaskCreateFromSelf(
    _ allocator: CFAllocator?
) -> SecTaskRef?

// NOTE: do NOT re-declare CFRelease with @_silgen_name. Unlike the SecTask* symbols above it is
// already imported from CoreFoundation, so a second declaration binds a different Swift type to
// the same linker symbol -- two SILFunctions with one name in one SILModule, which the optimizer
// can trip over under -O. (Calling the imported one is not an option either: Swift marks it
// unavailable, "Core Foundation objects are automatically memory managed".) Use Unmanaged instead.

@_silgen_name("SecTaskCopyValuesForEntitlements")
func SecTaskCopyValuesForEntitlements(
    _ task: SecTaskRef,
    _ entitlements: CFArray,
    _ error: UnsafeMutablePointer<Unmanaged<CFError>?>?
) -> CFDictionary?

// SecTaskRef is an OpaquePointer, so ARC does not own it: SecTaskCreateFromSelf hands back a +1
// reference that has to be released by hand. Unmanaged.release() is the CFRelease equivalent.
func releaseSecTask(_ task: SecTaskRef) {
    Unmanaged<AnyObject>.fromOpaque(UnsafeRawPointer(task)).release()
}

func checkAppEntitlements(_ ents: [String]) -> [String: Any] {
    guard let task = SecTaskCreateFromSelf(nil) else {
        return [:]
    }
    defer {
        releaseSecTask(task)
    }

    guard let entitlements = SecTaskCopyValuesForEntitlements(task, ents as CFArray, nil) else {
        return [:]
    }

    return (entitlements as NSDictionary) as? [String: Any] ?? [:]
}

func checkAppEntitlement(_ ent: String) -> Bool {
    guard let task = SecTaskCreateFromSelf(nil) else {
        return false
    }
    defer {
        releaseSecTask(task)
    }

    guard let entitlement = SecTaskCopyValueForEntitlement(task, ent as NSString, nil) else {
        return false
    }

    if let number = entitlement as? NSNumber {
        return number.boolValue
    } else if let bool = entitlement as? Bool {
        return bool
    }

    return false
}
