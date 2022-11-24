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
    * [Example](#example)
    * [Advantages and drawbacks](#advantages-and-drawbacks)
        + [Advantages](#advantages)
        + [Drawbacks](#drawbacks)
    * [Use-case scenarios](#use-case-scenarios)
    * [Caveas and gotchas](#caveas-and-gotchas)
    * [Tips and tricks](#tips-and-tricks)
        + [iOS](#ios)
    * [Links to repo's](#links-to-repo-s)

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

## Show me the code

(Optional) In your .fsproj:
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
(for iOS it DllName should be '__Internal', more on that below - you can `#if SOME_COMPILER_DIRECTIVE` to switch it in code conditionally)

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

## Example


Please look into `Program.fs` & corresponding `example` folder with C code in this repo-s directory.
It has been tested on macOS arm64 with `openssl` installed with `brew install openssl`.
But 
```bash
cmake -DOPENSSL_ROOT_DIR=<openssl_dir>  -DOPENSSL_LIBRARIES=<openssl_dir/lib>
make
```
should work anywhere.


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

## Caveas and gotchas

## Tips and tricks

### iOS 


## Links to repo's

http://www.fssnip.net/c1/title/F-yet-another-Interop-example

https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/functions/external-functions

https://fsharp.github.io/fsharp-core-docs/reference/fsharp-nativeinterop-nativeptrmodule.html

https://learn.microsoft.com/en-us/dotnet/standard/native-interop/