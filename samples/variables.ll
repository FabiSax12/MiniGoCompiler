; ModuleID = 'variables'
source_filename = "variables"

@x = internal global i32 42
@y = internal global double 3.140000e+00
@b = internal global i1 true
@println_fmt = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1
@println_fmt.1 = private unnamed_addr constant [4 x i8] c"%f\0A\00", align 1
@println_fmt.2 = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1
@println_fmt.3 = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1
@println_fmt.4 = private unnamed_addr constant [4 x i8] c"%f\0A\00", align 1
@println_fmt.5 = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1
@println_fmt.6 = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1

define void @main() {
entry:
  %a = alloca i32, align 4
  store i32 10, ptr %a, align 4
  %f = alloca double, align 8
  store double 2.710000e+00, ptr %f, align 8
  %flag = alloca i1, align 1
  store i1 false, ptr %flag, align 1
  %uninitialized = alloca i32, align 4
  store i32 0, ptr %uninitialized, align 4
  %x = load i32, ptr @x, align 4
  %0 = call i32 (ptr, ...) @printf(ptr @println_fmt, i32 %x)
  %y = load double, ptr @y, align 8
  %1 = call i32 (ptr, ...) @printf(ptr @println_fmt.1, double %y)
  %b = load i1, ptr @b, align 1
  %bool2int = zext i1 %b to i32
  %2 = call i32 (ptr, ...) @printf(ptr @println_fmt.2, i32 %bool2int)
  %a1 = load i32, ptr %a, align 4
  %3 = call i32 (ptr, ...) @printf(ptr @println_fmt.3, i32 %a1)
  %f2 = load double, ptr %f, align 8
  %4 = call i32 (ptr, ...) @printf(ptr @println_fmt.4, double %f2)
  %flag3 = load i1, ptr %flag, align 1
  %bool2int4 = zext i1 %flag3 to i32
  %5 = call i32 (ptr, ...) @printf(ptr @println_fmt.5, i32 %bool2int4)
  %uninitialized5 = load i32, ptr %uninitialized, align 4
  %6 = call i32 (ptr, ...) @printf(ptr @println_fmt.6, i32 %uninitialized5)
  ret void
}

declare i32 @printf(ptr, ...)
