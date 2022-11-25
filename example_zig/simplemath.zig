const std = @import("std");


export fn add(a: i32, b: i32) i32 {
    return a + b;
}
export fn mult(a: i32, b: i32) i32 {
  return a * b;
}

fn ziggZagg() !void {
  const stdout = std.io.getStdOut().writer();
  var i: usize = 1;
  while (i <= 16) : (i += 1) {
      if (i % 15 == 0) {
          try stdout.writeAll("ZiggZagg\n");
      } else if (i % 3 == 0) {
          try stdout.writeAll("Zigg\n");
      } else if (i % 5 == 0) {
          try stdout.writeAll("Zagg\n");
      } else {
          try stdout.print("{d}\n", .{i});
      }
  }
}
export fn ziggZaggTest() void {
  ziggZagg() catch {};
} 