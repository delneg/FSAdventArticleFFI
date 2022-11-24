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
    * [Show me the code](#show-me-the-code)
    * [Advantages and drawbacks](#advantages-and-drawbacks)
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

### Swift / ObjC / ObjC++

### Go

### Rust

### Node.js


## Show me the code

In your .fsproj:
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



And another example:

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




## Advantages and drawbacks

## Use-case scenarios

## Caveas and gotchas

## Tips and tricks

### iOS 


## Links to repo's

http://www.fssnip.net/c1/title/F-yet-another-Interop-example

https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/functions/external-functions

https://fsharp.github.io/fsharp-core-docs/reference/fsharp-nativeinterop-nativeptrmodule.html

https://learn.microsoft.com/en-us/dotnet/standard/native-interop/