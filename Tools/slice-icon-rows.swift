// Mechanical sprite extraction and downsampling. Source paintings remain intact.
import Foundation
import CoreGraphics
import ImageIO
import UniformTypeIdentifiers

let args = CommandLine.arguments
guard args.count == 7, let columns = Int(args[3]), let rows = Int(args[4]), let count = Int(args[5]),
      let source = CGImageSourceCreateWithURL(URL(fileURLWithPath: args[1]) as CFURL, nil),
      let atlas = CGImageSourceCreateImageAtIndex(source, 0, nil) else { fatalError("source output columns rows count") }
let cuts=args[6].split(separator:",").map{Int($0)!}
let output = URL(fileURLWithPath: args[2], isDirectory: true)
try FileManager.default.createDirectory(at: output, withIntermediateDirectories: true)
let size = 128
for index in 0..<count {
    let x0 = atlas.width * (index % columns) / columns
    let x1 = atlas.width * (index % columns + 1) / columns
    let y0 = cuts[index / columns]
    let y1 = cuts[index / columns + 1]
    guard let cell = atlas.cropping(to: CGRect(x: x0, y: y0, width: x1-x0, height: y1-y0)),
          let context = CGContext(data: nil, width: size, height: size, bitsPerComponent: 8, bytesPerRow: size*4,
                                  space: CGColorSpaceCreateDeviceRGB(), bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue) else { fatalError("cell") }
    context.interpolationQuality = .high
    let ratio=min(Double(size-16)/Double(cell.width),Double(size-16)/Double(cell.height))
    let w=Double(cell.width)*ratio,h=Double(cell.height)*ratio
    context.draw(cell, in: CGRect(x:(Double(size)-w)/2,y:(Double(size)-h)/2,width:w,height:h))
    let target = output.appendingPathComponent(String(format: "%02d.png", index))
    guard let image = context.makeImage(), let destination = CGImageDestinationCreateWithURL(target as CFURL, UTType.png.identifier as CFString, 1, nil) else { fatalError("destination") }
    CGImageDestinationAddImage(destination, image, nil)
    guard CGImageDestinationFinalize(destination) else { fatalError("write") }
    if count == 10 {
        let bytes = context.data!.bindMemory(to: UInt8.self, capacity: size*size*4)
        for pixel in 0..<(size*size) {
            let p = pixel*4
            let luminance = UInt8((54*Int(bytes[p]) + 183*Int(bytes[p+1]) + 19*Int(bytes[p+2])) / 256)
            bytes[p] = luminance; bytes[p+1] = luminance; bytes[p+2] = luminance
        }
        let dimTarget = output.appendingPathComponent(String(format: "%02d-cooldown.png", index))
        let dimDestination = CGImageDestinationCreateWithURL(dimTarget as CFURL, UTType.png.identifier as CFString, 1, nil)!
        CGImageDestinationAddImage(dimDestination, context.makeImage()!, nil)
        guard CGImageDestinationFinalize(dimDestination) else { fatalError("write cooldown") }
    }
}
print("Extracted \(count) RGBA icons, \(size)x\(size), from \(atlas.width)x\(atlas.height)")
