package main;

func testIfElse(x int) {
  if x > 0 {
    println(`positive`);
  };
  if x < 0 {
    println(`negative`);
  };
  if x == 0 {
    println(`zero`);
  };
};

func classicFor() {
  var i int = 0;
  for i = 0; i < 5; i = i + 1 {
    println(i);
  };
};

func whileStyleFor() {
  var count int = 3;
  for count > 0 {
    println(count);
    count = count - 1;
  };
};

func forWithoutInit() {
  var j int = 0;
  for j < 3 {
    println(j);
    j = j + 1;
  };
};

func main() {
  testIfElse(5);
  testIfElse(-3);
  testIfElse(0);

  classicFor();
  whileStyleFor();
  forWithoutInit();
};
