; ModuleID = 'arrays'
source_filename = "arrays"

@arr = internal global [5 x i32] zeroinitializer
@println_fmt = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1
@println_fmt.1 = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1
@println_fmt.2 = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1
@println_fmt.3 = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1
@println_fmt.4 = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1
@println_fmt.5 = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1
@println_fmt.6 = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1
@println_fmt.7 = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1

define void @main() {
entry:
  %local_arr = alloca [3 x i32], align 4
  store [3 x i32] zeroinitializer, ptr %local_arr, align 4
  store i32 10, ptr @arr, align 4
  store i32 20, ptr getelementptr ([5 x i32], ptr @arr, i32 0, i32 1), align 4
  store i32 30, ptr getelementptr ([5 x i32], ptr @arr, i32 0, i32 2), align 4
  store i32 40, ptr getelementptr ([5 x i32], ptr @arr, i32 0, i32 3), align 4
  store i32 50, ptr getelementptr ([5 x i32], ptr @arr, i32 0, i32 4), align 4
  %elem_ptr = getelementptr [3 x i32], ptr %local_arr, i32 0, i32 0
  store i32 100, ptr %elem_ptr, align 4
  %elem_ptr1 = getelementptr [3 x i32], ptr %local_arr, i32 0, i32 1
  store i32 200, ptr %elem_ptr1, align 4
  %elem_ptr2 = getelementptr [3 x i32], ptr %local_arr, i32 0, i32 2
  store i32 300, ptr %elem_ptr2, align 4
  %elem = load i32, ptr @arr, align 4
  %0 = call i32 (ptr, ...) @printf(ptr @println_fmt, i32 %elem)
  %elem3 = load i32, ptr getelementptr ([5 x i32], ptr @arr, i32 0, i32 1), align 4
  %1 = call i32 (ptr, ...) @printf(ptr @println_fmt.1, i32 %elem3)
  %elem4 = load i32, ptr getelementptr ([5 x i32], ptr @arr, i32 0, i32 2), align 4
  %2 = call i32 (ptr, ...) @printf(ptr @println_fmt.2, i32 %elem4)
  %elem5 = load i32, ptr getelementptr ([5 x i32], ptr @arr, i32 0, i32 3), align 4
  %3 = call i32 (ptr, ...) @printf(ptr @println_fmt.3, i32 %elem5)
  %elem6 = load i32, ptr getelementptr ([5 x i32], ptr @arr, i32 0, i32 4), align 4
  %4 = call i32 (ptr, ...) @printf(ptr @println_fmt.4, i32 %elem6)
  %5 = call i32 (ptr, ...) @printf(ptr @println_fmt.5, i32 5)
  %6 = call i32 (ptr, ...) @printf(ptr @println_fmt.6, i32 3)
  %i = alloca i32, align 4
  store i32 0, ptr %i, align 4
  br label %loop.cond

loop.cond:                                        ; preds = %loop.body, %entry
  %i7 = load i32, ptr %i, align 4
  %lt = icmp slt i32 %i7, 3
  br i1 %lt, label %loop.body, label %loop.exit

loop.body:                                        ; preds = %loop.cond
  %i8 = load i32, ptr %i, align 4
  %elem_ptr9 = getelementptr [3 x i32], ptr %local_arr, i32 0, i32 %i8
  %elem10 = load i32, ptr %elem_ptr9, align 4
  %7 = call i32 (ptr, ...) @printf(ptr @println_fmt.7, i32 %elem10)
  %i11 = load i32, ptr %i, align 4
  %add = add i32 %i11, 1
  store i32 %add, ptr %i, align 4
  br label %loop.cond

loop.exit:                                        ; preds = %loop.cond
  ret void
}

declare i32 @printf(ptr, ...)
