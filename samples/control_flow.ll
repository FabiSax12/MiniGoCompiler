; ModuleID = 'control_flow'
source_filename = "control_flow"

@rawstr = private unnamed_addr constant [9 x i8] c"positive\00", align 1
@println_fmt = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@rawstr.1 = private unnamed_addr constant [9 x i8] c"negative\00", align 1
@println_fmt.2 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@rawstr.3 = private unnamed_addr constant [5 x i8] c"zero\00", align 1
@println_fmt.4 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@println_fmt.5 = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1
@println_fmt.6 = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1
@println_fmt.7 = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1

define void @testIfElse(i32 %0) {
entry:
  %x = alloca i32, align 4
  store i32 %0, ptr %x, align 4
  %x1 = load i32, ptr %x, align 4
  %gt = icmp sgt i32 %x1, 0
  br i1 %gt, label %if.then, label %if.merge

if.then:                                          ; preds = %entry
  %1 = call i32 (ptr, ...) @printf(ptr @println_fmt, ptr @rawstr)
  br label %if.merge

if.merge:                                         ; preds = %if.then, %entry
  %x2 = load i32, ptr %x, align 4
  %lt = icmp slt i32 %x2, 0
  br i1 %lt, label %if.then3, label %if.merge4

if.then3:                                         ; preds = %if.merge
  %2 = call i32 (ptr, ...) @printf(ptr @println_fmt.2, ptr @rawstr.1)
  br label %if.merge4

if.merge4:                                        ; preds = %if.then3, %if.merge
  %x5 = load i32, ptr %x, align 4
  %eq = icmp eq i32 %x5, 0
  br i1 %eq, label %if.then6, label %if.merge7

if.then6:                                         ; preds = %if.merge4
  %3 = call i32 (ptr, ...) @printf(ptr @println_fmt.4, ptr @rawstr.3)
  br label %if.merge7

if.merge7:                                        ; preds = %if.then6, %if.merge4
  ret void
}

declare i32 @printf(ptr, ...)

define void @classicFor() {
entry:
  %i = alloca i32, align 4
  store i32 0, ptr %i, align 4
  store i32 0, ptr %i, align 4
  br label %loop.cond

loop.cond:                                        ; preds = %loop.body, %entry
  %i1 = load i32, ptr %i, align 4
  %lt = icmp slt i32 %i1, 5
  br i1 %lt, label %loop.body, label %loop.exit

loop.body:                                        ; preds = %loop.cond
  %i2 = load i32, ptr %i, align 4
  %0 = call i32 (ptr, ...) @printf(ptr @println_fmt.5, i32 %i2)
  %i3 = load i32, ptr %i, align 4
  %add = add i32 %i3, 1
  store i32 %add, ptr %i, align 4
  br label %loop.cond

loop.exit:                                        ; preds = %loop.cond
  ret void
}

define void @whileStyleFor() {
entry:
  %count = alloca i32, align 4
  store i32 3, ptr %count, align 4
  br label %loop.cond

loop.cond:                                        ; preds = %loop.body, %entry
  %count1 = load i32, ptr %count, align 4
  %gt = icmp sgt i32 %count1, 0
  br i1 %gt, label %loop.body, label %loop.exit

loop.body:                                        ; preds = %loop.cond
  %count2 = load i32, ptr %count, align 4
  %0 = call i32 (ptr, ...) @printf(ptr @println_fmt.6, i32 %count2)
  %count3 = load i32, ptr %count, align 4
  %sub = sub i32 %count3, 1
  store i32 %sub, ptr %count, align 4
  br label %loop.cond

loop.exit:                                        ; preds = %loop.cond
  ret void
}

define void @forWithoutInit() {
entry:
  %j = alloca i32, align 4
  store i32 0, ptr %j, align 4
  br label %loop.cond

loop.cond:                                        ; preds = %loop.body, %entry
  %j1 = load i32, ptr %j, align 4
  %lt = icmp slt i32 %j1, 3
  br i1 %lt, label %loop.body, label %loop.exit

loop.body:                                        ; preds = %loop.cond
  %j2 = load i32, ptr %j, align 4
  %0 = call i32 (ptr, ...) @printf(ptr @println_fmt.7, i32 %j2)
  %j3 = load i32, ptr %j, align 4
  %add = add i32 %j3, 1
  store i32 %add, ptr %j, align 4
  br label %loop.cond

loop.exit:                                        ; preds = %loop.cond
  ret void
}

define void @main() {
entry:
  call void @testIfElse(i32 5)
  call void @testIfElse(i32 -3)
  call void @testIfElse(i32 0)
  call void @classicFor()
  call void @whileStyleFor()
  call void @forWithoutInit()
  ret void
}
