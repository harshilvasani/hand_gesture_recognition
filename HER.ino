#include <IRremote.h>
#include <LiquidCrystal.h>

LiquidCrystal lcd(12, 11, 5, 4, 3, 2);
 
IRsend irsend;
char incomingByte;
int hand_shake=0,x=0;

void setup()
{
  lcd.begin(16, 2);
  Serial.begin(9600);
  hand_shake=0;
  
  lcd.noDisplay();
}

void loop() 
{ 
      if(Serial.available()>0)
      {
        incomingByte = Serial.read();
            
        if(hand_shake==0)//while com port not found
        {
          hand_shake=1;
          delay(3000); 
          Serial.write(incomingByte);
        }
        else//com port found
        {
          if(incomingByte=='N')
          {
            x=0;
            lcd.noDisplay();
          }
            
          else if(incomingByte=='D')
          {
            lcd.clear();
            lcd.print("Danger Zone...");
            lcd.setCursor(0,1);
            lcd.print("get back in");
            
            lcd.setCursor(12,1);
            
            lcd.print((int)(18-x)/2);
            x=x+1;
            lcd.setCursor(13,1);
            lcd.print("sec");
            lcd.display();
          }
            
          if(incomingByte=='s')//to restart
             hand_shake=0;
          
          else if(incomingByte=='P')
          {
             lcd.clear();
             lcd.print("POWER");
             lcd.display();
             irsend.sendNEC(0x80BF3BC4,32);//PWER
             //lcd.print("power");
          }
          
          else if(incomingByte=='X')
          {
              //lcd.setCursor(0,1);
             lcd.clear();
             lcd.print("Mute");
             lcd.display();
             irsend.sendNEC(0x80BF39C6,32);//MUTE
          }
             
          else if(incomingByte=='G')
          {
              lcd.clear();
             lcd.print("GUIDE");
             lcd.display();
             irsend.sendNEC(0x80BF5BA4,32);//GUIDE
          //   lcd.print("GUIDE");
          }
             
          else if(incomingByte=='M')
          {    
             lcd.clear();
             lcd.print("MENU");
             lcd.display();
             irsend.sendNEC(0x80BF19E6,32);//MENU
          }
          
          else if(incomingByte=='1')
          {
             lcd.clear();
             lcd.print("button 1");
             lcd.display();
             irsend.sendNEC(0x80BF49B6,32);//
          }
             
          else if(incomingByte=='2')
          {
             lcd.clear();
             lcd.print("button 2");
             lcd.display();
             irsend.sendNEC(0x80BFC936,32);//
          }
             
          else if(incomingByte=='3')
          {
             lcd.clear();
             lcd.print("button 3");
             lcd.display();
             irsend.sendNEC(0x80BF33CC,32);//
          }
             
          else if(incomingByte=='4')
          {
             lcd.clear();
             lcd.print("button 4");
             lcd.display();
             irsend.sendNEC(0x80BF718E,32);//
          }
             
          else if(incomingByte=='5')
          {
             lcd.clear();
             lcd.print("button 5");
             lcd.display();
             irsend.sendNEC(0x80BFF10E,32);//
          }
             
          else if(incomingByte=='6')
          {
              lcd.clear();
             lcd.print("button 6");
             lcd.display();
             irsend.sendNEC(0x80BF13EC,32);//
          }
             
          else if(incomingByte=='7')
          {
             lcd.clear();
             lcd.print("button 7");
             lcd.display();
             irsend.sendNEC(0x80BF51AE,32);//
          }
             
          else if(incomingByte=='8')
          {
             lcd.clear();
             lcd.print("button 8");
             lcd.display();
             irsend.sendNEC(0x80BFD12E,32);//
          }
             
          else if(incomingByte=='9')
          {
             lcd.clear();
             lcd.print("button 9");
             lcd.display();
             irsend.sendNEC(0x80BF23DC,32);//
          }
             
          else if(incomingByte=='0')
          {  
             lcd.clear();
             lcd.print("button 0");
             lcd.display();
             irsend.sendNEC(0x80BFE11E,32);//
          }
             
          else if(incomingByte=='F')
          {
             lcd.clear();
             lcd.print("Favorite");
             lcd.display();
             irsend.sendNEC(0x80BF830C,32);//FAV
          }
             
          else if(incomingByte=='L')
          {
             lcd.clear();
             lcd.print("Last");
             lcd.display();
             irsend.sendNEC(0x80BF41BE,32);//LAST
          }
             
          else if(incomingByte=='+')
          {
             lcd.clear();
             lcd.print("VOL +");
             lcd.display();
             irsend.sendNEC(0x80BF01FE,32);//VOL +
          }
             
          else if(incomingByte=='-')
          {
             lcd.clear();
             lcd.print("VOL -");
             lcd.display();
             irsend.sendNEC(0x80BF817E,32);//VOL -
          }
             
          else if(incomingByte=='*')
          {
             lcd.clear();
             lcd.print("CH +");
             lcd.display();
             irsend.sendNEC(0x80BFA15E,32);//CH +
          }
             
          else if(incomingByte=='/')
          {
             lcd.clear();
             lcd.print("CH -");
             lcd.display();
             irsend.sendNEC(0x80BF619E,32);//CH -
          }
             
          else if(incomingByte=='o')
          {
             lcd.clear();
             lcd.print("OK");
             lcd.display();
             irsend.sendNEC(0x80BF738C,32);//OK
          }  
             
          else if(incomingByte=='^')
          {
             lcd.clear();
             lcd.print("UP");
             lcd.display();
             irsend.sendNEC(0x80BF53AC,32);//UP
          }
             
          else if(incomingByte=='v')
          {
             lcd.clear();
             lcd.print("DOWN");
             lcd.display();
             irsend.sendNEC(0x80BF4BB4,32);//DOWN
          }
             
          else if(incomingByte=='<')
          {
             lcd.clear();
             lcd.print("LEFT");
             lcd.display();
             irsend.sendNEC(0x80BF9966,32);//LEFT
          }
             
          else if(incomingByte=='>')
          {
             lcd.clear();
             lcd.print("RIGHT");
             lcd.display();
             irsend.sendNEC(0x80BF837C,32);//RIGHT
          }
             
          else if(incomingByte=='E')
          {
             lcd.clear();
             lcd.print("EXIT");
             lcd.display();
             irsend.sendNEC(0x80BFA35C,32);//EXIT
          }
             
          else if(incomingByte=='R')
          {
             lcd.clear();
             lcd.print("RED");
             lcd.display();
             irsend.sendNEC(0x80BF916E,32);//RED
          }
             
          else if(incomingByte=='B')
          {
             lcd.clear();
             lcd.print("BLUE");
             lcd.display();
             irsend.sendNEC(0x80BF6996,32);//BLUE  
          }
             
          else if(incomingByte=='b')
             irsend.sendNEC(0x80BF43BC,32);//Back rajkot remote
        }  
      }   
}
 //irsend.sendNEC(0x80BF4BB4,32);//ok down
 //irsend.sendNEC(0x80BF619E,32);//channel down
       
