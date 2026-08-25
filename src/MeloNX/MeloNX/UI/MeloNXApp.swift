//
//  MeloNXApp.swift
//  MeloNX
//
//  Created by Stossy11 on 4/4/2026.
//

import SwiftUI
import SDL3
import AVFoundation

func configureAudioSession() {
    do {
        let session = AVAudioSession.sharedInstance()

        try session.setCategory(
            .playback,
            options: .mixWithOthers
        )
        try session.setPreferredSampleRate(48000)
        try session.setPreferredIOBufferDuration(0.005)
        try session.setActive(true)
    } catch {
        print("Audio session error: \(error.localizedDescription)")
    }
}

var environment: [EnvironmentVariable] = [
    EnvironmentVariable(string: "MVK_CONFIG_MAX_ACTIVE_METAL_COMMAND_BUFFERS_PER_QUEUE", value: "128"),
]


func initEnvironmentVariables() {
    if let device = MTLCreateSystemDefaultDevice(), device.argumentBuffersSupport.rawValue >= MTLArgumentBuffersTier.tier2.rawValue {
        environment.append(contentsOf: [
            .init(string: "MVK_CONFIG_USE_METAL_ARGUMENT_BUFFERS", value: "0")
        ])
    }
    
    if #available(iOS 19, *) {
        environment.append(contentsOf: [
            .init(string: "HAS_TXM", value: ProcessInfo.processInfo.hasTXM && !ProcessInfo.processInfo.isiOSAppOnMac ? "1" : "0"),
            .init(string: "DUAL_MAPPED_JIT", value: "1")
        ])
    }
    
    for env in environment { env.set() }
}

class AppDelegate: NSObject, UIApplicationDelegate {
    static var orientationLock = UIInterfaceOrientationMask.all

    func application(_ application: UIApplication, supportedInterfaceOrientationsFor window: UIWindow?) -> UIInterfaceOrientationMask {
        return AppDelegate.orientationLock
    }
}


@main
struct MeloNXApp: App {
    @UIApplicationDelegateAdaptor(AppDelegate.self) var appDelegate
    
    @StateObject var ryujinxController: RyujinxController = .shared
    @StateObject var themeManager: ThemeManager = .shared
    
    @AppStorage("hasSetupFinished") var hasSetupFinished: Bool = false
    @AppStorage("lastAppversion") var lastAppversion: Data = Data()
    
    init() {
        SDL_SetMainReady()
        SDL_SetiOSEventPump(true)
        configureAudioSession()
        SDL_Init(SDL_INIT_EVENTS | SDL_INIT_AUDIO)
        configureAudioSession()
        JIT26BreakpointHandler()
        initEnvironmentVariables()
        Ryujinx.initialize()
        ThemeManager.shared.applyUIKitAppearance()
    }
    
    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(ryujinxController)
                .environmentObject(themeManager)
                .withAppTheme()
                .onAppear() {
                    UIDevice.current.beginGeneratingDeviceOrientationNotifications()
                    
                    let versionNumber = decodeFloatFromData(lastAppversion)

                    if versionNumber < Float(Bundle.main.versionNumber) ?? .zero {
                        lastAppversion = encodeFloatToData(Float(Bundle.main.versionNumber) ?? .zero)
                         hasSetupFinished = false
                    }
                }
                .sheet(isPresented: .constant(!hasSetupFinished)) {
                    SetupView {
                        hasSetupFinished = true
                        ryujinxController.loadGames()
                    }
                    .interactiveDismissDisabled()
                    .withAppTheme()
                }
        }
    }
    
    func encodeFloatToData(_ value: Float) -> Data {
        var mutableValue = value
        return Data(bytes: &mutableValue, count: MemoryLayout<Float>.size)
    }

    /// Reads back a value written by `encodeFloatToData`.
    ///
    /// On a fresh install the stored Data is empty, and loading a Float from zero
    /// bytes trips `UnsafeRawBufferPointer.load`'s bounds check under -Onone. A
    /// missing or short value reads as 0, which is older than any real version, so
    /// setup runs again. The bytes are reassembled by hand so this needs no
    /// alignment guarantee and no unsafe pointer access.
    func decodeFloatFromData(_ data: Data) -> Float {
        guard data.count >= MemoryLayout<Float>.size else { return .zero }

        let start = data.startIndex
        let bits = UInt32(data[start])
            | UInt32(data[start + 1]) << 8
            | UInt32(data[start + 2]) << 16
            | UInt32(data[start + 3]) << 24

        return Float(bitPattern: bits)
    }
}
