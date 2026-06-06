; ModuleID = 'hello'
source_filename = "hello"

@rawstr = private unnamed_addr constant [15 x i8] c"Hello, MiniGo!\00", align 1
@println_fmt = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1

define void @main() {
entry:
  %0 = call i32 (ptr, ...) @printf(ptr @println_fmt, ptr @rawstr)
  ret void
}

declare i32 @printf(ptr, ...)
