# Communicating with other languages and runtimes, aka FFI in F#

- [Communicating with other languages and runtimes, aka FFI in F#](#communicating-with-other-languages-and-runtimes--aka-ffi-in-f-)
    * [What is FFI](#what-is-ffi)
    * [Reasons to do FFI](#reasons-to-do-ffi)
    * [Decision making](#decision-making)
    * [Comparison with other languages / runtimes methods to do FFI](#comparison-with-other-languages---runtimes-methods-to-do-ffi)
        + [Java & JVM](#java---jvm)
        + [Swift / ObjC / ObjC++](#swift---objc---objc--)
        + [Go](#go)
        + [Rust](#rust)
        + [Node.js](#nodejs)
        + [Others](#others)
    * [Show me the code](#show-me-the-code)
    * [Examples](#examples)
        + [C](#c)
        + [Rust](#rust-1)
        + [Zig](#zig)
        + [Swift](#swift)
    * [Advantages and drawbacks](#advantages-and-drawbacks)
        + [Advantages](#advantages)
        + [Drawbacks](#drawbacks)
    * [Use-case scenarios](#use-case-scenarios)
    * [Caveas and gotchas](#caveas-and-gotchas)
    * [Tips and tricks](#tips-and-tricks)
    * [Useful links](#useful-links)

**DISCLAIMER:**

All of the code & text that's in the repo and in the README is based on my own experience, and as such may be prone to error, misunderstanding or other flaws.
Use at your own risk.


## What is FFI

**[I don't want to read all of it - just show me the code!](#show-me-the-code)**

When developing applications for server, mobile , web or embedded platforms one might often 
find oneself in need of extra functionality, that may be unavailable in the language that software is developed in or have the need to tap into
other language / runtime's capabilities for various other reasons.
That's not a new problem, and has existed for quite a while.
In fact, according to [wikipedia](https://en.wikipedia.org/wiki/Foreign_function_interface)
"The term comes from the specification for Common Lisp, which explicitly refers to the language features for inter-language calls as such;"

Although the FFI should not be confused with languages that operate on the same runtime - 
for example, C# / F# interoperability is not actually FFI because they compile to IL.
The same argument is true for Java / Kotlin / Scala / Clojure , as well as Erlang / Elixir ([wiki](https://en.wikipedia.org/wiki/Foreign_function_interface#Special_cases))

Terminology and disambiguation:
- In dotnet FFI is also known by the name of P/Invoke.
- Managed code - is 'home' language / runtime, unmanaged code (or 'native' code) - code after FFI bridge, i.e. C/C++/Rust code
- Runtime / platform - things like BEAM (Erlang), CLR (dotnet), JVM (java) etc.
- Name mangling - means the names of exported functions are changed and become something like `_Z13lib_exported`


MSDN link: https://learn.microsoft.com/en-us/dotnet/standard/native-interop/

## Reasons to do FFI


You might ask yourself - "Why do I even need to do FFI ? I can just re-write it in ~~Rust~~ F# (or C#, or VB) and use it that way!"

And while dotnet has one of __the best__ (in my opinion) FFI interoperability, 
you will need some convincing for youself (or your colleagues) to dive deep into it.

Well, there can be several good (in my opinion) reasons.


**First of all**, the functionality you need may simply not be available 
in your language and may be cumbersome / time-consuming / error-prone to re-write it from scratch.
For example, you should probably not rewrite cryptography functions like the ones availiable in `openssl` or `boringssl` or `libressl`, because they've been battle-tested and highly optimized.


**Secondly**, you might want to have better performance. Of course, there is certain overhead associated with FFI calls (you might want to check out [this repo, although it's quite outdated](https://github.com/dyu/ffi-overhead))
However, even with overhead (which might not matter in your case) - it might be much faster and / or memory-efficient than managed language implementation.
[Here's](https://github.com/ncave/fable-raytracer/issues/1) one of the example where that might be the case - although not strictly "FFI" related, the numbers can be interesting, as well as the repo itself.

**And last but not least**, you might want to build a shared code-base to be able to re-use it across different languages, platforms and runtimes.
For example, you might have a business with a C# Xamarin/Maui app for iOS and Android, Web app written in React, Desktop app utilizing Electron with React app sharing code with the web app,
and a CLI written in Rust, with backend written in F# Asp.Net Core.

So, it might be a good idea to de-couple the logic from the CLI into shared Rust library, 
which can be built by CI to be re-used across mobile app, web app & desktop app via WASM, CLI app as a Cargo crate and the backend & mobile apps via dotnet FFI (also called P/Invoke).
An example of a similar setup can be found on github of messaging app called Signal - https://github.com/signalapp/libsignal

## Decision making

After all, it all comes down to a decision - to be or not to be (or, in this case - to do FFI or keep it managed)
These are the criteria that I might use in order to make such a decision:

1. **Ease of consumption**

_Will my FFI-enabled code be easy to consume for my clients (other developers)_ ?

If you're developing a public dotnet library on github, it can be challenging to make it accessible to broad range of platforms that dotnet runs on,
because you'll need to compile your FFI library independently from your dotnet code. A good example is [this BLAKE3 hashing library](https://github.dev/xoofx/Blake3.NET). 
It has a Rust crate `blake3` wrapped in a FFI-friendly package, together with CI-friendly build scripts, and has pre-built versions for Linux, macOS and Windows for x86_64, arm and arm64 architectures.

However, if you're planning to use your FFI code in company project for backend app running on x86_64 Linux with a recent Kernel, that may be totally not a concern for you.

2. **Rewrite possibility in managed code**

_Maybe it's better to write the desired functionality in F# / C# after all ?_

Sometimes, that's simply not possible. In another case, you might be better off having a well-performing & optimized managed implementation,
than trying to debug another "segmentation fault. core dumped" error.


3. **Small FFI surface**
 
_Will I be able to keep the exposed "unsafe" / "private" surface of the FFI-code small & approachable ?_

Perhaps, sometime after your code using FFI will be landed in production, someone else will have to take a look at it - 
and it may be very difficult to change some FFI-related code without knowing it's purpose by trying to check for exposed headers (`nm -d libfoo.so`) 
and guessing what the various flags passed to the function do.

4. **Application safety & stability**

_Did I manage to wrap the FFI code in a way that's safe (first of all, memory-safe) and secure (i.e. introduces no new vulnerabilities and doesn't lead to unexpected crashes) ?_

To help make the answer to this question a confident "Yes", you might employ the help of such tools like Unit tests, Integration tests and Fuzzer testing.
Also, it could be beneficial to utilize tools like `memory sanitizer` and / or `valgrind` in order to detect memory leaks early, as well as the wide range of dotnet-specific tools available.


## Comparison with other languages / runtimes methods to do FFI

### Java & JVM

In the JVM world, FFI is mainly done using JNI ([guide](https://www.baeldung.com/jni))
which required creating an intermediary C code using Java types.

It typically looks like this:
```c

#include <stdio.h>
#include <sys/time.h>

#include "jhello_Hello.h"
#include "../newplus/plus.h"

JNIEXPORT jint JNICALL Java_jhello_Hello_plusone
  (JNIEnv *env, jclass clazz, jint x)
{
    return plusone(x);
    //return x + 1;
}

...
```

```java
package jhello;

public final class Hello
{
    public static native int plusone(int x);

    private static void loadNative() throws Exception
    {
        java.io.File file = new java.io.File("."), 
            jhello = new java.io.File(file, "jhello");
        
        if (jhello.exists())
            file = jhello;
        
        String currentDir = file.getCanonicalPath();
    
        System.load(currentDir + "/libjhello.so");
    }
    public static void main(String[] args) throws Exception
    {
        // load
        loadNative();
        plusone(5);
    }
    
```

However, there's a recent development called [Project Panama](https://openjdk.org/projects/panama/) (a k a [JEP-424](https://openjdk.org/jeps/424))
An example of using those can be observed here: https://github.com/cryptomator/jfuse


### Swift / ObjC / ObjC++


Swift [can interoperate with C](https://developer.apple.com/documentation/swift/using-imported-c-functions-in-swift) (and other Cdecl-enabled FFI languages) directly using header files


Swift also supports [C structs and enums (unions)](https://developer.apple.com/documentation/swift/using-imported-c-structs-and-unions-in-swift).

However,it can sometimes be not practical - sometimes, it's easier to create an Objective-C "bridge" to provide a nice API's for both C code and Swift code.
An example of that can be seen in my [AVIF image format decoder repo](https://github.com/delneg/Nuke-AVIF-Plugin/), and specifically [here's what Swift consumes](https://github.com/delneg/Nuke-AVIF-Plugin/blob/main/Source/AVIF/AVIFImageMacros.h)

Also, there are projects like [this, that utilize Swift stable ABI to create a direct bridge between Swift <-> Rust](https://github.com/chinedufn/swift-bridge)

Regarding the C++, ObjC++ can interop with it directly - so it's typically quite practical to
implement some C++ API Surface to be consumed by ObjC++, which is in turn consumed by a Swift wrapper.
An [example project, although a bit outdated - can be found here](https://github.com/sitepoint-editors/HelloCpp)


There's a long & extensive [dedicated document made by a specialized workgroup regarding Swift <-> C++ interop](https://github.com/apple/swift/blob/main/docs/CppInteroperability/CppInteroperabilityManifesto.md)


### Go

In Go, [there's a special package, that allows developers to use C (or Cdecl-compatible code) from Go, called `cgo`](https://pkg.go.dev/cmd/cgo).

Here's a [guide on CGo](https://zchee.github.io/golang-wiki/cgo/)

Here's an [example project, integrating Go and Rust together](https://github.com/mediremi/rust-plus-golang)

There's [a long and opinionated article on Go in general and CGo in particular, which shows the quirks of the approach](https://fasterthanli.me/articles/i-want-off-mr-golangs-wild-ride)
(there's [a follow-up post, which includes a part related to CGo](https://fasterthanli.me/articles/lies-we-tell-ourselves-to-keep-using-golang#go-is-an-island))

However, [CGo is not Go](https://dave.cheney.net/2016/01/18/cgo-is-not-go) and while it can be used for FFI, has constraints and limitations, as well as practical issues.

In addition to that, you have to specify [linker flags](https://github.com/winfsp/cgofuse/blob/master/fuse/host_cgo.go#L19), [OS-specific build instructions](https://dh1tw.de/2019/12/cross-compiling-golang-cgo-projects/) and [includes using a special syntax in the comments.](https://akrennmair.github.io/golang-cgo-slides/#9)

It looks something like this:
```c
void hello(char *name);
void whisper(char *message);
```

```go
package main

// NOTE: There should be NO space between the comments and the `import "C"` line.

/*
#cgo LDFLAGS: -L./lib -lhello
#include "./lib/hello.h"
*/
import "C"

func main() {
    C.hello(C.CString("world"))
    C.whisper(C.CString("this is code from the dynamic library"))
}
```
```bash
go build -ldflags="-r $(ROOT_DIR)lib" main_dynamic.go
```

### Rust

Because of the nature of Rust (borrow-checking memory management, lack of runtime and GC , suitable type system, etc.), it's very easy to interop with it using other languages.

Notably, [there's a project from Mozilla that creates a unified interface for Rust to be used from multiple languages](https://mozilla.github.io/uniffi-rs/Overview.html)

There's a project to automatically generate Rust code from C/C++ headers [called rust-bindgen](https://github.com/rust-lang/rust-bindgen), 
as well as [it's counterpart for vice versa - to generate C/C++ headers for Rust code](https://github.com/eqrion/cbindgen)

There are quite a lot of articles:
- [in the Rustonomicon](https://doc.rust-lang.org/nomicon/ffi.html)
- [Michael F Bryan blog Rust ffi guide](https://michael-f-bryan.github.io/rust-ffi-guide/)
- [Secure Rust Guidelines ffi guide](https://anssi-fr.github.io/rust-guide/07_ffi.html)
and so on.

It looks mainly like this:
```rust
use std::os::raw::c_int;
// import an external function from libc
extern "C" {
    fn abs(args: c_int) -> c_int;
}
// export a C-compatible function
#[no_mangle]
unsafe extern "C" fn mylib_f(param: u32) -> i32 {
    if param == 0xCAFEBABE { 0 } else { -1 }
}
```


### Node.js

Because Node.js runs on top of V8, an execution engine written in C++ and due to JS being an interpreted language, it's pretty easy to dynamically import C code.

It's mainly done using the [node-ffi library](https://github.com/node-ffi/node-ffi), which has a nice [tutorial here](https://github.com/node-ffi/node-ffi/wiki/Node-FFI-Tutorial)

The code looks something like this:
```javascript
var ref = require('ref');
var ffi = require('ffi');

// typedef
var sqlite3 = ref.types.void; // we don't know what the layout of "sqlite3" looks like
var sqlite3Ptr = ref.refType(sqlite3);
var sqlite3PtrPtr = ref.refType(sqlite3Ptr);
var stringPtr = ref.refType(ref.types.CString);

// binding to a few "libsqlite3" functions...
var libsqlite3 = ffi.Library('libsqlite3', {
  'sqlite3_open': [ 'int', [ 'string', sqlite3PtrPtr ] ],
  'sqlite3_close': [ 'int', [ sqlite3Ptr ] ],
  'sqlite3_exec': [ 'int', [ sqlite3Ptr, 'string', 'pointer', 'pointer', stringPtr ] ],
  'sqlite3_changes': [ 'int', [ sqlite3Ptr ]]
});

// now use them:
var dbPtrPtr = ref.alloc(sqlite3PtrPtr);
libsqlite3.sqlite3_open("test.sqlite3", dbPtrPtr);
var dbHandle = dbPtrPtr.deref();
```

There's also a [neat wrapper, called node-ffi-napi](https://github.com/node-ffi-napi/node-ffi-napi) which you can use.

In addition to that, [you can use Node.js headers to write Node.js native modules directly](https://blog.risingstack.com/writing-native-node-js-modules/),
also called native addons. An [example project of Rust native module can be seen in here](https://blog.logrocket.com/rust-and-node-js-a-match-made-in-heaven/)
And a [Rust project that simplifies writing native modules](https://github.com/napi-rs/napi-rs) as well as alternatives like [node-bindgen](https://github.com/infinyon/node-bindgen)


Because the Javascript is a browser language, Node.js also supports Webassembly (WASM), 
which can be used to simplify running untrusted code in a constrained environment or to compile native code (C/C++/Rust) to performant WASM.

An example of such usage [with WAT text code format can be seen here, with benchmarks against other possible use cases](https://github.com/bengl/sbffi/blob/master/test/bench.js#L19)


### Others

Although I've covered quite a few languages that I've had experience with, there's definitely more to it - for example, I left out BEAM languages, as well as Python.
After all, this is an article about FFI in dotnet - and mainly it's usage with F#.


You might be interested [in automated binding generator - although at the moment of writing this, it only supports C#](https://github.com/ralfbiedert/interoptopus). 
It's possible to generate C# bindings in a separate project and use that in F# (or re-write only the needed parts in F# by hand).



## Show me the code

(Optional, skip if your target platform is macos/linux/windows and not xamarin) In your .fsproj:
```xml
  <ItemGroup>
    <NativeReference Include=".\rust-src\libfoo">
      <Kind>Static</Kind>
      <IsCxx>False</IsCxx>
      <ForceLoad>False</ForceLoad>
    </NativeReference>
  </ItemGroup>
```

In your .fs file:
```fsharp
module Native =
    let [<Literal>] DllName = "libfoo"
    
    [<DllImport(DllName, CallingConvention=CallingConvention.Cdecl)>]
    //(value1: i32, value2: i32) -> i32
    extern int32 add_values(int32 value1, int32 value2)
    
    
    <DllImport(DllName, CallingConvention=CallingConvention.Cdecl)>]
    //(x: i32, y: i32, w: i32, h: i32, angle: f64) -> *const u8
    extern IntPtr render_scene(int x, int y, int w, int h, float angle)
```
(for iOS it DllName should be '__Internal', [more on that here](https://learn.microsoft.com/en-us/xamarin/ios/platform/native-interop?source=recommendations#static-libraries)  - you can `#if SOME_COMPILER_DIRECTIVE` to switch it in code conditionally)

Calling:

```fsharp
printfn $"Native add_values: 5 + 6 = {Native.add_values(5,6)}"
let ret_ptr = Native.render_scene(x,y,w,h,angle)
let ptr_hex = String.Format("{0:X8}", ret_ptr.ToInt64())
printfn $"Native big render scene returned 0x{ptr_hex}"
```


And another, more complex example:

```fsharp
module Native =
 [<Literal>]
 let DllName = "libfoo.so";
 [<Literal>]
 let FOO_BAR_SIZE = 1337 // FOO_BAR struct size in bytes


 [<DllImport(DllName, CallingConvention=CallingConvention.Cdecl)>]
 extern int Set_Library_Ptr(byte[] ptrData, int flags, [<In>]IntPtr ptrValue)
 [<DllImport(DllName, CallingConvention=CallingConvention.Cdecl)>]
 extern unit Make_Expensive_computation(byte[] inbuf, byte[] outbuf, uint64 length,[<In>]IntPtr ptrValue, int flag)


 let nativeWrapper (data: byte[]) (flag: int) (byteBuf: byte[])  =
   let intPtr = NativePtr.stackalloc<byte> FOO_BAR_SIZE |> NativePtr.toNativeInt
   let mutable res = Array.zeroCreate data.Length
   let set_res = Set_Library_Ptr(byteBuf, 64, intPtr)
   Make_Expensive_computation(data, res, (uint64 data.Length), intPtr, flag)
   res
```


[Type marshalling](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/type-marshalling#default-rules-for-marshalling-common-types)

## Examples


### C

Please look into `Program.fs` & corresponding `example_c` folder with C code in this repo-s directory.
It has been tested on macOS arm64 with `openssl` installed with `brew install openssl`.
But 
```bash
cmake -DOPENSSL_ROOT_DIR=<openssl_dir>  -DOPENSSL_LIBRARIES=<openssl_dir/lib>
make
```
should work anywhere.

The goal of the program is to encrypt a string using AES XTS encryption, and then decrypt it to check if it's gone through a round trip correctly.

When run, the program outputs the following result:
```
Will encode :'Calling C from F#'
Encryption key:'MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDEyMw==', initialization vector: 'MDEyMzQ1Njc4OTAxMjM0NQ=='
Ciphertext[17]: 63630A73F208EF8CC3ECE1937AFCDB61210000000000000000000000000000000000 - encrypted in 46ms
Plaintext[17]: Calling C from F# - decrypted in 46ms
```

### Rust

Feel free to check out my demo of Rust <-> F# interop running on iOS here:

https://github.com/delneg/fable-raytracer-ios-net6

Despite the name, it was recently updated to dotnet 7.

The interesting part is passing the pointer Rust -> F# -> ObjC platform code (to [CGDataProvider](https://developer.apple.com/documentation/coregraphics/cgdataprovider))

P.S. I'm not sure this code is memory-safe though, so if you find a potential leak, please ping me.

```rust
#[no_mangle]
pub unsafe extern "C" fn render_scene(x: i32, y: i32, w: i32, h: i32, angle: f64) -> *const u8 {
    let buffer = get_buffer();
    RayTracerDemo::renderScene(&buffer, &x, &y, &w, &h, &angle);
    buffer.as_ptr()
}
```

```fsharp
module Native =
    let [<Literal>] DllName = "__Internal"
    
    [<DllImport(DllName, CallingConvention=CallingConvention.Cdecl)>]
    extern IntPtr render_scene(nativeint x, nativeint y, nativeint w, nativeint h, float angle)
    
    let getUIImageForRGBAData width height (dataPtr:IntPtr) (dataLen:int) =
        // https://gist.github.com/irskep/e560be65163efcb04115
        let bytesPerPixel = 4
        let scanWidth = bytesPerPixel * width
        let provider = new CGDataProvider(dataPtr, dataLen)
        let colorSpaceRef = CGColorSpace.CreateDeviceRGB()
        let bitMapInfo = CGBitmapFlags.Last
        let renderingIntent = CGColorRenderingIntent.Default
        let imageRef = new CGImage(width,height,8, bytesPerPixel * 8, scanWidth, colorSpaceRef, bitMapInfo, provider,null,false,renderingIntent)
        new UIImage(imageRef)

...

let ret_ptr = Native.render_scene(nativeint x,nativeint y,nativeint w,nativeint h,angle)
let ptr_hex = String.Format("{0:X8}", ret_ptr.ToInt64())
printfn $"Native big render scene returned 0x{ptr_hex}"
let imageView = new UIImageView()
imageView.Frame <- CGRect(float x, float y, float w, float h)
imageView.Image <- Native.getUIImageForRGBAData w h ret_ptr len
this.View.AddSubview(imageView)
```



### Zig

There's a simple Zig example in this repo, which can be built with

`zig build-lib simplemath.zig -dynamic`

```zig
export fn add(a: i32, b: i32) i32 {
    return a + b;
}
export fn mult(a: i32, b: i32) i32 {
  return a * b;
}
export fn ziggZaggTest() void {
  ziggZagg() catch {};
} 
```

```fsharp
module Native_Zig =
    let [<Literal>] DllName = "example_zig/libsimplemath"
    
    [<DllImport(DllName, CallingConvention=CallingConvention.Cdecl)>]
    extern int add(int a, int b)
    
    [<DllImport(DllName, CallingConvention=CallingConvention.Cdecl)>]
    extern int mult(int a, int b)
    
    [<DllImport(DllName, CallingConvention=CallingConvention.Cdecl)>]
    extern unit ziggZaggTest()
    
printfn $"Zig add 5 + 6 = {Native_Zig.add (5,6)} , mult 11 * 42 = {Native_Zig.mult (11,42)}"
printfn "Zig zagg test"
Native_Zig.ziggZaggTest()
```

Output:
```bash
Zig add 5 + 6 = 11 , mult 11 * 42 = 462
Zig zagg test
1
2
Zigg
4
Zagg
Zigg
...
```

### Swift

[It turns out](https://gist.github.com/HiImJulien/c79f07a8a619431b88ea33cca51de787), you can compile Swift code to shared library and execute it normally, utilizing the "hidden" `@_cdecl()` attribute.

```swift
import Foundation
@_cdecl("say_hello")
public func say_hello(){
    print("Hello from Swift!")
}

@_cdecl("advanced_random")
public func advanced_random(num: Int, num2: Int) -> Int {
  return Int.random(in: num..<num2)
}
```
```fsharp
module Native_Swift =
    let [<Literal>] DllName = "example_swift/libfunc"
    
    [<DllImport(DllName, CallingConvention=CallingConvention.Cdecl)>]
    extern int advanced_random(int num, int num2)
    
    [<DllImport(DllName, CallingConvention=CallingConvention.Cdecl)>]
    extern unit say_hello()
    
Native_Swift.say_hello()
printfn $"Random number from Swift: {Native_Swift.advanced_random(1,100)}"
```

Output:
```bash
Hello from Swift!
Random number from Swift: 21
```

## Advantages and drawbacks

### Advantages

- Utilize existing libraries in other languages
- Harness the power of raw assembly via C / Rust easily
- Share the code with other teams / projects 
- Make "hot paths" in the code execute natively, and be able to perform low-level optimizations where it matters
- Tools & interface for profiling, which are closer to "bare metal"
- Do things like Xamarin (call ObjC or JNI from dotnet) and calling Linux / Windows / MacOS / BSD OS-level functions
- Tap into hardware-specific functionalities (i.e. SIMD instructions, although these already have a lot of nice wrappers)

### Drawbacks

- Platform (os, architecture, kernel, platform restrictions) incompatibilities
- Much harder build process (no more simple "dotnet run" unless you spend time to automate it)
- Overhead is present, and it depends on the target platform & runtime etc.
- Adds a lot of complexity for an average dotnet developer
- Very easy to shoot yourself in the foot (leaks, crashes, vulns, etc.)


## Use-case scenarios

- Web Apps in WASM - [Blazor WASM native dependencies](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-native-dependencies?view=aspnetcore-7.0#use-native-code)
- Mobile apps [Android](https://learn.microsoft.com/en-us/xamarin/android/platform/native-libraries) and [iOS](https://learn.microsoft.com/en-us/xamarin/ios/platform/native-interop)
- Backends (code in Examples)
- Performance optimizations
- Re-using existing libraries - like [SQLite wrapper in C#](https://github.com/praeclarum/sqlite-net/blob/master/src/SQLite.cs#L4448)
- Access platform-specific libraries like [libsoundio wrapper in Zig](https://ziglang.org/learn/overview/#integration-with-c-libraries-without-ffibindings)

## Caveas and gotchas

- AFAIK, currently you can not use a static library on regular dotnet (that is, non-Xamarin or something else). 
I may be wrong though, but generally look for .so / .dylib compilation - shared libraries work even without added code to .fsproj
- `NativeFileReference` <> `NativeReference` - small difference in spelling, but totally different meaning
- On iOS / macOS there _are a few extra steps_:
  - `MonoTouch.ObjCRuntime.Dlfcn.dlopen ("/full/path/to/Animal.dylib", 0);` if using a dylib
  - `__Internal` in DllImport - that means, you can't use more than one static library that way (which is fine in most cases). Solution to that problem ? Pack them into .framework or .xcframework
- Library names search [differs on different platforms](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/cross-platform?source=recommendations), all that .dylib / .so / .dll you see
- Starting with .NET Core 3.1, you can write [custom import resolver to search for your library path](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/cross-platform#custom-import-resolver)
- AFAIK, the source generation for `LibraryImport` [attribute, added in dotnet 7](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke-source-generation), only works for C# code


## Tips and tricks

- If you want to inspect your DLL that you've built, check out http://penet.io/
- Native interop [best practices](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/best-practices)
- If you want to explore the native library that you've build in C/C++/Rust/Go/Zig/etc., use `otool -TV lib.dylib` or `nm -gU lib.dylib` for macOS, `dumpbin /EXPORTS lib.dll` for Windows, and `readelf -s lib.so` or `nm -D lib.so` for Linux (or `nm -gDC lib.so` for demangled version)
- You can do it the other way around, [here's a blog post on calling F# from C++](https://secanablog.wordpress.com/2020/02/01/writing-a-native-library-in-f-which-can-be-called-from-c/)
and [here's the code](https://github.com/secana/Native-FSharp-Library)
- A few functions and namespaces that you might find useful:
  ```fsharp
  NativePtr.stackalloc
  GC.AllocateUninitializedArray
  Marshal.AllocHGlobal
  Marshal.FreeHGlobal
  NativePtr.toNativeInt
  NativePtr.ofNativeInt
  NativePtr.nullPtr
  Marshal.PtrToStringAnsi
  ArrayPool.Shared.Rent
  ArrayPool.Shared.Return
  GC.KeepAlive
  fixed keyword
  Unchecked.defaultof<'T>
  ```
- https://godbolt.org/ - compiler explorer
- [Byref](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/byrefs), a k a "by reference" (opposed to "by value")
- Using native libs in [Xamarin](https://learn.microsoft.com/en-us/xamarin/cross-platform/cpp/)
- Sandboxing native code - running WASM code in WASI environment in an Avalonia app via [Wasmtime](https://github.com/bytecodealliance/wasmtime) runtime [link to repo](https://github.com/delneg/WasmtimeFableRaytracerFSharpAvalonia)
- Inline ASM in F# [post](https://blog.devgenius.io/inline-assembly-in-f-net-language-6d70ab9f58c1?gi=2a8fb0a2ffa8)
- A guide on [interop attributes](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/apply-interop-attributes)



## Useful links

http://www.fssnip.net/c1/title/F-yet-another-Interop-example

https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/functions/external-functions

https://fsharp.github.io/fsharp-core-docs/reference/fsharp-nativeinterop-nativeptrmodule.html

https://learn.microsoft.com/en-us/dotnet/standard/native-interop/

https://learn.microsoft.com/en-us/dotnet/standard/native-interop/type-marshalling#default-rules-for-marshalling-common-types

https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/fixed

https://github.com/swig/swig

https://learn.microsoft.com/en-us/xamarin/android/platform/native-libraries
https://learn.microsoft.com/en-us/xamarin/ios/platform/native-interop

https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.nativelibrary?view=net-7.0

