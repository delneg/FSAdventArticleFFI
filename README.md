# Communicating with other languages and runtimes, aka FFI in F#


## What is FFI

When developing applications for server, mobile , web or embedded platforms one might often 
find oneself in need of extra functionality, that may be unavailable in the language that software is developed in or have the need to tap into
other language / runtime's capabilities for various other reasons.
That's not a new problem, and has existed for quite a while.
In fact, according to [wikipedia](https://en.wikipedia.org/wiki/Foreign_function_interface)
"The term comes from the specification for Common Lisp, which explicitly refers to the language features for inter-language calls as such;"

Although the FFI should not be confused with languages that operate on the same runtime - 
for example, C# / F# interoperability is not actually FFI because they compile to IL.
The same argument is true for Java / Kotlin / Scala / Clojure , as well as Erlang / Elixir ([wiki](https://en.wikipedia.org/wiki/Foreign_function_interface#Special_cases))

In dotnet FFI is also known by the name of P/Invoke.


## Reasons to do FFI


You might ask yourself - "Why do I even need to do FFI ? I can just re-write it in ~~Rust~~ F# (or C#, or VB) and use it that way!"

Well, there can be several good (in my opinion) reasons.


1. First of all, the functionality you need may simply not be available 
in your language and may be cumbersome / time-consuming / error-prone to re-write it from scratch.
For example, you should probably not rewrite cryptography functions like the ones availiable in `openssl` or `boringssl` or `libressl`, because they've been battle-tested and highly optimized.


2. Secondly, you might want to have better performance. Of course, there is certain overhead associated with FFI calls (you might want to check out [this repo, although it's quite outdated](https://github.com/dyu/ffi-overhead))
However, even with overhead (which might not matter in your case) - it might be much faster and / or memory-efficient than managed language implementation.

[Here's](https://github.com/ncave/fable-raytracer/issues/1) one of the example where that might be the case - although not strictly "FFI" related, the numbers can be interesting, as well as the repo itself.


3. And last but not least, you might want to build a shared code-base to be able to re-use it across different languages, platforms and runtimes.
For example, you might have a business with a C# Xamarin/Maui app for iOS and Android, Web app written in React, Desktop app utilizing Electron with React app sharing code with the web app,
and a CLI written in Rust, with backend written in F# Asp.Net Core.

So, it might be a good idea to de-couple the logic from the CLI into shared Rust library, 
which can be built by CI to be re-used across mobile app, web app & desktop app via WASM, CLI app as a Cargo crate and the backend & mobile apps via dotnet FFI (also called P/Invoke).
An example of a similar setup can be found on github of messaging app called Signal - https://github.com/signalapp/libsignal





## Comparison with other languages / runtimes methods to do FFI

### Java & JVM

### Swift / ObjC / ObjC++

### Go

### Rust

### Node.js




## Advantages and drawbacks

## Use-case scenarios

## Caveas and gotchas

## Tips and tricks

### iOS 

## Links to repo's