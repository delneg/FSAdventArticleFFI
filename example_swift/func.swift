import Foundation


@_cdecl("say_hello")
public func say_hello(){
    print("Hello from Swift!")
}

@_cdecl("advanced_random")
public func advanced_random(num: Int, num2: Int) -> Int {
  return Int.random(in: num..<num2)
}
