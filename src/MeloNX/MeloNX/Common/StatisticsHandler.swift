//
//  StatisticsHandler.swift
//  MeloNX
//
//  Created by Stossy11 on 26/5/2026.
//

import SwiftUI
import Foundation
import Combine

private struct StatisticsRequest: Codable {
    let FPS: Double
    let FrameTime: Double
    let Started: Bool
    let FIFO: Double
}

class StatisticsHandler: ObservableObject {
    @Published var fps: Double = 0
    @Published var frameTime: Double = 0
    @Published var started: Bool = false
    @Published var fifo: Double = 0
    
    func registerPush() {
        CallbackManager.register(name: "push_statistics") { data in
            guard let ptr = data.ptr, data.len >= 33 else { return }

            // The payload is packed, so FIFO sits at offset 17 and is never
            // 8-byte aligned. `load` requires alignment, so copy the bytes out
            // instead of reinterpreting them in place.
            var fps = 0.0
            var frameTime = 0.0
            var startedByte: UInt8 = 0
            var fifo = 0.0

            memcpy(&fps, ptr, MemoryLayout<Double>.size)
            memcpy(&frameTime, ptr + 8, MemoryLayout<Double>.size)
            memcpy(&startedByte, ptr + 16, MemoryLayout<UInt8>.size)
            memcpy(&fifo, ptr + 17, MemoryLayout<Double>.size)

            let started = startedByte != 0

            Task {
                await MainActor.run {
                    self.fps       = fps
                    self.frameTime = frameTime
                    self.started   = started
                    self.fifo      = fifo
                }
            }
        }
    }
    
    func formatFPS() -> String {
        String(format: "FPS: %.2f", fps)
    }
}
