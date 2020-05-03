class classone{
  public void firstclass(){
    return;
  }
  public int secondclass(){
    int a,b,c;

    a=5;
    b=10;
    c=a*b;
    writeln(c);
    return c;
  }
}
final class Main{
  public static void main(String [] args){
    classone.secondclass();
  }
}
