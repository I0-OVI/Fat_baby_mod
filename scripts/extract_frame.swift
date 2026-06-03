#!/usr/bin/env swift
import AVFoundation
import AppKit
import Foundation

func usage() -> Never {
    fputs("""
    Usage:
      extract_frame.swift <video> <output.png> [--at SECONDS | --at-pct PERCENT | --duration]
      extract_frame.swift <video> <output_dir> --probe-picks [--probe-step N] [--probe-max N] [--trim SECONDS]

    Probe picks: skip first/last trim (default 1s), then sample every probe-step % within the window.

    Examples:
      extract_frame.swift 攻击.mp4 basic_attack.png --at-pct 50
      extract_frame.swift 攻击.mp4 picks/basic_attack --probe-picks

    """, stderr)
    exit(1)
}

func writePNG(_ cgImage: CGImage, to outputURL: URL) throws {
    let image = NSImage(cgImage: cgImage, size: NSSize(width: cgImage.width, height: cgImage.height))
    guard let tiff = image.tiffRepresentation,
          let bitmap = NSBitmapImageRep(data: tiff),
          let png = bitmap.representation(using: .png, properties: [:]) else {
        throw NSError(domain: "extract_frame", code: 3, userInfo: [NSLocalizedDescriptionKey: "failed to encode png"])
    }
    try FileManager.default.createDirectory(
        at: outputURL.deletingLastPathComponent(),
        withIntermediateDirectories: true
    )
    try png.write(to: outputURL)
}

let args = Array(CommandLine.arguments.dropFirst())
guard args.count >= 2 else { usage() }

let videoPath = args[0]
let outputPath = args[1]
var atSeconds: Double?
var atPercent: Double?
var durationOnly = false
var probePicks = false
var probeStep = 2.0
var probeMax = 96.0
var trimMargin = 1.0

var index = 2
while index < args.count {
    switch args[index] {
    case "--at":
        index += 1
        guard index < args.count, let value = Double(args[index]) else { usage() }
        atSeconds = value
    case "--at-pct":
        index += 1
        guard index < args.count, let value = Double(args[index]) else { usage() }
        atPercent = value
    case "--duration":
        durationOnly = true
    case "--probe-picks", "--probe-batch":
        probePicks = true
    case "--probe-step":
        index += 1
        guard index < args.count, let value = Double(args[index]) else { usage() }
        probeStep = value
    case "--probe-max":
        index += 1
        guard index < args.count, let value = Double(args[index]) else { usage() }
        probeMax = value
    case "--trim":
        index += 1
        guard index < args.count, let value = Double(args[index]) else { usage() }
        trimMargin = value
    default:
        usage()
    }
    index += 1
}

let videoURL = URL(fileURLWithPath: videoPath)
let asset = AVURLAsset(url: videoURL)
let total = CMTimeGetSeconds(asset.duration)

if durationOnly {
    guard total.isFinite, total > 0 else {
        fputs("error: could not read video duration\n", stderr)
        exit(2)
    }
    print(String(format: "%.3f", total))
    exit(0)
}

guard total.isFinite, total > 0 else {
    fputs("error: could not read video duration\n", stderr)
    exit(2)
}

let generator = AVAssetImageGenerator(asset: asset)
generator.appliesPreferredTrackTransform = true
generator.requestedTimeToleranceBefore = .zero
generator.requestedTimeToleranceAfter = .zero

if probePicks {
    let trimStart = trimMargin
    let trimEnd = total - trimMargin
    guard trimEnd > trimStart else {
        fputs("error: video too short for \(trimMargin)s trim on each end (duration \(total)s)\n", stderr)
        exit(2)
    }
    let span = trimEnd - trimStart
    let step = probeStep
    var pcts: [Double] = []
    var pct = step
    while pct <= probeMax + 0.001 {
        pcts.append(pct)
        pct += step
    }

    let outDir = URL(fileURLWithPath: outputPath, isDirectory: true)
    try FileManager.default.createDirectory(at: outDir, withIntermediateDirectories: true)

    var manifestLines = [
        "# \(URL(fileURLWithPath: videoPath).lastPathComponent) — \(pcts.count) frames",
        "# duration: \(String(format: "%.2f", total))s | trim: skip first/last \(String(format: "%.0f", trimMargin))s",
        "# window: \(String(format: "%.2f", trimStart))s – \(String(format: "%.2f", trimEnd))s | step: \(Int(step))% of window",
        "# columns: file | pct_in_window | time_s",
        ""
    ]

    for (i, pct) in pcts.enumerated() {
        let targetSeconds = trimStart + span * (pct / 100.0)
        let time = CMTime(seconds: targetSeconds, preferredTimescale: 600)
        var actualTime = CMTime.zero

        do {
            let cgImage = try generator.copyCGImage(at: time, actualTime: &actualTime)
            let fileName: String
            if step < 1.0 {
                let pctLabel = String(format: "%03d", Int((pct * 10.0).rounded()))
                fileName = String(format: "%03d_pct%@.png", i + 1, pctLabel)
            } else {
                let pctLabel = String(format: "%02d", Int(pct.rounded()))
                fileName = String(format: "%03d_pct%@.png", i + 1, pctLabel)
            }
            let fileURL = outDir.appendingPathComponent(fileName)
            try writePNG(cgImage, to: fileURL)
            let actualS = CMTimeGetSeconds(actualTime)
            manifestLines.append(String(format: "%@ | %3.0f%% | %.2fs", fileName, pct, actualS))
            print("wrote \(fileURL.path) (window \(Int(pct))%, \(String(format: "%.2f", actualS))s)")
        } catch {
            fputs("error at window \(Int(pct))%: \(error.localizedDescription)\n", stderr)
            exit(4)
        }
    }

    let manifestURL = outDir.appendingPathComponent("README.txt")
    try manifestLines.joined(separator: "\n").write(to: manifestURL, atomically: true, encoding: .utf8)
    print("manifest=\(manifestURL.path)")
    exit(0)
}

let targetSeconds: Double
if let atSeconds {
    targetSeconds = atSeconds
} else {
    let pct = atPercent ?? 50.0
    targetSeconds = total * (pct / 100.0)
}

let time = CMTime(seconds: targetSeconds, preferredTimescale: 600)
var actualTime = CMTime.zero

do {
    let cgImage = try generator.copyCGImage(at: time, actualTime: &actualTime)
    if outputPath != "/dev/null" {
        try writePNG(cgImage, to: URL(fileURLWithPath: outputPath))
    }
    print("video=\(videoPath)")
    print("output=\(outputPath)")
    print("duration_s=\(String(format: "%.3f", total))")
    print("requested_s=\(String(format: "%.3f", targetSeconds))")
    print("actual_s=\(String(format: "%.3f", CMTimeGetSeconds(actualTime)))")
    print("size=\(cgImage.width)x\(cgImage.height)")
} catch {
    fputs("error: \(error.localizedDescription)\n", stderr)
    exit(4)
}
