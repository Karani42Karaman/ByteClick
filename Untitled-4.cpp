#include <Trade/Trade.mqh>
CTrade trade;

string buyPanel="BUY_PANEL";
string sellPanel="SELL_PANEL";

string buyBtn="BUY_BTN";
string sellBtn="SELL_BTN";

string lotBox="LOT_BOX";
string tpBox="TP_BOX";

double pip;

// --- URL'ler (Postman'deki gibi) ---
string api_url      = "http://127.0.0.1:7021/webhook/tradingview/latest";
string log_open_url = "http://127.0.0.1:7021/webhook/tradingview/open";
string log_close_url = "http://127.0.0.1:7021/webhook/tradingview/close";


//---------------- INIT ----------------//
int OnInit()
{
   if(_Digits==5 || _Digits==3)
      pip=10*_Point;
   else
      pip=_Point;

   //---------------- BUY PANEL ----------------//
   ObjectCreate(0,buyPanel,OBJ_RECTANGLE_LABEL,0,0,0);
   ObjectSetInteger(0,buyPanel,OBJPROP_XDISTANCE,10);
   ObjectSetInteger(0,buyPanel,OBJPROP_YDISTANCE,10);
   ObjectSetInteger(0,buyPanel,OBJPROP_XSIZE,140);
   ObjectSetInteger(0,buyPanel,OBJPROP_YSIZE,80);
   ObjectSetInteger(0,buyPanel,OBJPROP_BGCOLOR,clrDarkGreen);
   ObjectSetInteger(0,buyPanel,OBJPROP_BORDER_COLOR,clrWhite);

   //---------------- BUY BUTTON ----------------//
   ObjectCreate(0,buyBtn,OBJ_BUTTON,0,0,0);

   ObjectSetInteger(0,buyBtn,OBJPROP_XDISTANCE,25);
   ObjectSetInteger(0,buyBtn,OBJPROP_YDISTANCE,30);

   ObjectSetInteger(0,buyBtn,OBJPROP_XSIZE,110);
   ObjectSetInteger(0,buyBtn,OBJPROP_YSIZE,40);

   ObjectSetString(0,buyBtn,OBJPROP_TEXT,"BUY");

   ObjectSetInteger(0,buyBtn,OBJPROP_BGCOLOR,clrLimeGreen);
   ObjectSetInteger(0,buyBtn,OBJPROP_COLOR,clrWhite);
   ObjectSetInteger(0,buyBtn,OBJPROP_FONTSIZE,14);

   //---------------- SELL PANEL ----------------//
   ObjectCreate(0,sellPanel,OBJ_RECTANGLE_LABEL,0,0,0);

   ObjectSetInteger(0,sellPanel,OBJPROP_XDISTANCE,170);
   ObjectSetInteger(0,sellPanel,OBJPROP_YDISTANCE,10);

   ObjectSetInteger(0,sellPanel,OBJPROP_XSIZE,140);
   ObjectSetInteger(0,sellPanel,OBJPROP_YSIZE,80);

   ObjectSetInteger(0,sellPanel,OBJPROP_BGCOLOR,clrDarkRed);
   ObjectSetInteger(0,sellPanel,OBJPROP_BORDER_COLOR,clrWhite);

   //---------------- SELL BUTTON ----------------//
   ObjectCreate(0,sellBtn,OBJ_BUTTON,0,0,0);

   ObjectSetInteger(0,sellBtn,OBJPROP_XDISTANCE,185);
   ObjectSetInteger(0,sellBtn,OBJPROP_YDISTANCE,30);

   ObjectSetInteger(0,sellBtn,OBJPROP_XSIZE,110);
   ObjectSetInteger(0,sellBtn,OBJPROP_YSIZE,40);

   ObjectSetString(0,sellBtn,OBJPROP_TEXT,"SELL");

   ObjectSetInteger(0,sellBtn,OBJPROP_BGCOLOR,clrRed);
   ObjectSetInteger(0,sellBtn,OBJPROP_COLOR,clrWhite);
   ObjectSetInteger(0,sellBtn,OBJPROP_FONTSIZE,14);

   //---------------- LOT BOX ----------------//
   CreateEdit(lotBox,"0.01",20,110);

   //---------------- TP BOX ----------------//
   CreateEdit(tpBox,"20",170,110);
   EventSetTimer(1);
   Print("Sistem Hazir");
   return(INIT_SUCCEEDED);
}
void OnTimer()
{
    char post[], result[];
    string headers = "";
    int res = WebRequest("GET", api_url, headers, 2000, post, result, headers);
    if(res == 200)
    {
        string response = CharArrayToString(result, 0, WHOLE_ARRAY, CP_UTF8);
        StringToUpper(response);
        if(StringFind(response, "\"ACTION\":\"BUY\"") >= 0) OpenBuy();
        else if(StringFind(response, "\"ACTION\":\"SELL\"") >= 0) OpenSell();
    }
}

//---------------- UI ----------------//
void CreateEdit(string name,string val,int x,int y)
{
   ObjectCreate(0,name,OBJ_EDIT,0,0,0);

   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,x);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,y);

   ObjectSetInteger(0,name,OBJPROP_XSIZE,120);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,30);

   ObjectSetString(0,name,OBJPROP_TEXT,val);

   ObjectSetInteger(0,name,OBJPROP_FONTSIZE,12);
}

//---------------- CLICK ----------------//
void OnChartEvent(const int id,const long &l,const double &d,const string &s)
{
   if(id==CHARTEVENT_OBJECT_CLICK)
   {
      if(s==buyBtn) OpenBuy();
      if(s==sellBtn) OpenSell();
   }
}

//---------------- BUY ----------------//
void OpenBuy()
{
   if(!TerminalInfoInteger(TERMINAL_TRADE_ALLOWED))
   {
      Print("TRADE İZİN YOK");
      return;
   }

   double lot = StringToDouble(ObjectGetString(0,lotBox,OBJPROP_TEXT));
   double tpPip = StringToDouble(ObjectGetString(0,tpBox,OBJPROP_TEXT));
   double ask = SymbolInfoDouble(_Symbol,SYMBOL_ASK);
   double tp = ask + tpPip * pip;
   tp = NormalizeDouble(tp,_Digits);

   if(!trade.Buy(lot,_Symbol,ask,0,tp))
   {
      Print("BUY HATA: ",trade.ResultRetcode());
      Print("AÇIKLAMA: ",trade.ResultRetcodeDescription());
   }
   else{
      ulong ticket = trade.ResultOrder();
        if(ticket <= 0) ticket = trade.ResultOrder();
        
        string json = StringFormat("{\"ticket\":%d,\"symbol\":\"%s\",\"type\":\"BUY\",\"lot\":%.2f,\"openPrice\":%.5f}", 
                                    (int)ticket, _Symbol, lot, ask);
        SendToWeb(log_open_url, json);
        Print("BUY AÇILDI");
   }
   
      
}

//---------------- SELL ----------------//
void OpenSell()
{
   if(!TerminalInfoInteger(TERMINAL_TRADE_ALLOWED))
   {
      Print("TRADE İZİN YOK");
      return;
   }

   double lot = StringToDouble(ObjectGetString(0,lotBox,OBJPROP_TEXT));
   double tpPip = StringToDouble(ObjectGetString(0,tpBox,OBJPROP_TEXT));

   double bid = SymbolInfoDouble(_Symbol,SYMBOL_BID);

   double tp = bid - tpPip * pip;
   tp = NormalizeDouble(tp,_Digits);

   if(!trade.Sell(lot,_Symbol,bid,0,tp))
   {
      Print("SELL HATA: ",trade.ResultRetcode());
      Print("AÇIKLAMA: ",trade.ResultRetcodeDescription());
   }
   else{
        ulong ticket = trade.ResultDeal();
        if(ticket <= 0) ticket = trade.ResultOrder();
        
        string json = StringFormat("{\"ticket\":%d,\"symbol\":\"%s\",\"type\":\"SELL\",\"lot\":%.2f,\"openPrice\":%.5f}", 
                                    (int)ticket, _Symbol, lot, bid);
        SendToWeb(log_close_url, json);
        Print("SELL AÇILDI");
   }
      
}

void OnTradeTransaction(const MqlTradeTransaction& trans, const MqlTradeRequest& request, const MqlTradeResult& result)
{
    if(trans.type == TRADE_TRANSACTION_DEAL_ADD && HistoryDealSelect(trans.deal))
    {
        long pos_id = HistoryDealGetInteger(trans.deal, DEAL_POSITION_ID);
        double profit = HistoryDealGetDouble(trans.deal, DEAL_PROFIT);
        double price = HistoryDealGetDouble(trans.deal, DEAL_PRICE);
        if(MathAbs(profit) > 0.00001)
        {
            string json = StringFormat("{\"ticket\":%d,\"closePrice\":%.5f,\"profit\":%.2f}", (int)pos_id, price, profit);
            SendToWeb(log_close_url, json);
        }
    }
}