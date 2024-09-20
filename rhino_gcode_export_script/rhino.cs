using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Text;
using System.IO;
/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public class Script_Instance : GH_ScriptInstance
{
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { /* Implementation hidden. */ }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { /* Implementation hidden. */ }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { /* Implementation hidden. */ }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { /* Implementation hidden. */ }

  /// <summary>Gets the current Rhino document.</summary>
  private readonly RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private readonly GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private readonly IGH_Component Component;
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private readonly int Iteration;

  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments,
  /// Output parameters as ref arguments. You don't have to assign output parameters,
  /// they will have a default value.
  /// </summary>
  private void RunScript(string FilePath, bool Apply, DataTree<Vector3d> BasePoints, DataTree<Vector3d> OuterMostPillars, DataTree<Vector3d> PillarPoints, DataTree<Vector3d> HairPoints, double LayerHeight, int HairSpeed, double Amount, List<string> ParameterInfo, int LayerSkip, out object A)
  {

    if(!Apply){
      return;
    }


    _LayerHeight = LayerHeight;
    _NozzleWidth = 0.4;
    _RetAmount = 6.0;
    _RetSpeed = 1200;
    int PrintSpeed = 500;
    int PrintSpeedHigh = 1000;

    StringBuilder sb = new StringBuilder();

    makeGcodeInfo(sb, ParameterInfo);

    sb.Append("G28\n");
    sb.Append("M83\n");
    sb.Append("G90\n");
    sb.Append("G1 F300 Z0.4\n");
    sb.Append("G1 X50 E8 F800\n");
    sb.Append(makeRetraction(_RetAmount, _RetSpeed, -1));

    double CurrentHeight = 0;

    for(int i = 0; i < BasePoints.Paths.Count; i++){
      List<Vector3d> pList = BasePoints.Branch(i);

      if(pList.Count != 0){
        sb.Append(makeGcode(pList[0]));
        if(i == 0){
          sb.Append(makeRetraction(_RetAmount, _RetSpeed, 1));
        }
        for(int j = 0; j < pList.Count; j++){
          if(j == 0){
            CurrentHeight = pList[0].Z;
            sb.Append("G1 F300 Z" + CurrentHeight.ToString("F3") + "\n");

            sb.Append(makeGcode(pList[j]));
            continue;
          }
          if(pList[j].Z > CurrentHeight + (_LayerHeight / 10)){
            CurrentHeight = pList[j].Z;
            sb.Append("G1 F300 Z" + CurrentHeight.ToString("F3") + "\n");
          }
          sb.Append(makeGcode(pList[j - 1], pList[j], PrintSpeedHigh));
        }
        sb.Append(makeGcode(pList[pList.Count - 1], pList[0], PrintSpeedHigh));
      }
    }
    sb.Append(makeRetraction(_RetAmount, _RetSpeed, -1));

    // Pillar generation
    int GAP = 3;
    int LayerNum = 0;
    int HairNum = 0;

    for(int i = 0; i < PillarPoints.Paths.Count; i++){

      List<Vector3d> pList = PillarPoints.Branch(i);

      if(LayerNum != PillarPoints.Paths[i].Indices[0]){
        LayerNum = PillarPoints.Paths[i].Indices[0];
        Print("" + LayerNum);

        // Outermost pillars
        int iii = LayerNum - GAP;
        if(iii >= 0){
          if(iii >= 5 && iii < 4800){

            // Hair
            List<Vector3d> HairList = new List<Vector3d>();
            if(HairNum % (LayerSkip * 2) == 0){
              HairList = HairPoints.Branch(0);
            }else if (HairNum % LayerSkip == 0){
              HairList = HairPoints.Branch(1);
            }else{

            }

            for(int j = 0; j < HairList.Count; j++){
              if(j == 0){
                sb.Append("; Hair =================== \n");
                sb.Append("G91\n");
                sb.Append("G1 X3\n");
                sb.Append("G90\n");
                sb.Append("G1 F9000 Y" + (HairList[j].Y - 2).ToString("F2") + "\n");
                sb.Append("G1 F9000 X" + (HairList[j].X).ToString("F2") + "\n");
                sb.Append(makeGcode(HairList[j]));
                sb.Append("G1 F300 Z" + (CurrentHeight - ((GAP - 2) * _LayerHeight)) + "\n");
                sb.Append(makeRetraction((_RetAmount), _RetSpeed, 1));
                sb.Append("G4 P1000\n");

                continue;
              }

              // ****************************

              sb.Append(makeHairGcode(HairList[j - 1], HairList[j], Amount, HairSpeed));

              // ****************************

            }
            if(HairList.Count != 0){
              sb.Append("G91\n");
              sb.Append("G1 Y2\n");
              sb.Append("G90\n");
              sb.Append(makeRetraction(1, _RetSpeed, 1));
              sb.Append("G4 P1000\n");
              sb.Append(makeRetraction(_RetAmount, _RetSpeed, -1));

            }
            HairNum++;
          }

          List<Vector3d> OuterMostList = OuterMostPillars.Branch(iii);

          for(int j = 0; j < OuterMostList.Count; j++){
            if(j == 0){
              double BothHeight = OuterMostList[0].Z;
              sb.Append("G91\n");
              sb.Append("G1 X5\n");
              sb.Append("G90\n");
              sb.Append(makeGcode(OuterMostList[j]));
              sb.Append("G1 F300 Z" + BothHeight + "\n");
              sb.Append(makeRetraction(_RetAmount + 0.5, _RetSpeed, 1));
              continue;
            }
            if(j == OuterMostList.Count / 2){
              sb.Append(makeGcode(OuterMostList[j - 1], OuterMostList[0], PrintSpeed));
              sb.Append(makeRetraction(_RetAmount, _RetSpeed, -1));
              sb.Append("G91\n");
              sb.Append("G1 X3\n");
              sb.Append("G90\n");
              sb.Append("G1 F9000 Y" + OuterMostList[j].Y.ToString("F2") + "\n");
              sb.Append(makeGcode(OuterMostList[j]));
              sb.Append(makeRetraction(_RetAmount, _RetSpeed, 1));
              continue;
            }
            sb.Append(makeGcode(OuterMostList[j - 1], OuterMostList[j], PrintSpeedHigh));
          }
          sb.Append(makeGcode(OuterMostList[OuterMostList.Count - 1], OuterMostList[OuterMostList.Count / 2], PrintSpeed));

          sb.Append(makeRetraction(_RetAmount, _RetSpeed, -1));
          sb.Append("G1 F300 Z" + CurrentHeight + "\n");

          sb.Append("G1 F9000 Y" + pList[0].Y.ToString("F2") + "\n");
        }
      }

      //sb.Append("G1 X3\n");

      if(pList.Count != 0){

        CurrentHeight = pList[0].Z;
        sb.Append("G1 F300 Z" + CurrentHeight + "\n");
        sb.Append(makeGcode(pList[0]));
        sb.Append(makeRetraction(_RetAmount, _RetSpeed, 1));


        for(int j = 0; j < pList.Count; j++){
          if(j == 0){
            //sb.Append("G4 P100\n");
            sb.Append(makeGcode(pList[j]));
            continue;
          }
          if(pList[j].Z > CurrentHeight + (_LayerHeight / 10)){
            CurrentHeight = pList[j].Z;
            sb.Append("G1 F300 Z" + CurrentHeight.ToString("F3") + "\n");
          }
          sb.Append(makeGcode(pList[j - 1], pList[j], PrintSpeed));
        }
        sb.Append(makeGcode(pList[pList.Count - 1], pList[0], PrintSpeed));
      }
      sb.Append(makeRetraction(_RetAmount, _RetSpeed, -1));

    }




    sb.Append(makeRetraction(_RetAmount, _RetSpeed, -1));
    CurrentHeight += 10;
    sb.Append("G1 Z" + CurrentHeight.ToString("F3") + "\n");


    sb.Append("M84");

    WriteFile(sb.ToString(), FilePath);


  }

  // <Custom additional code> 
  double _LayerHeight;
  double _NozzleWidth;
  double _RetAmount;
  int _RetSpeed;

  void makeGcodeInfo(StringBuilder sb, List<string> p){
    sb.Append("; " + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "\n");
    sb.Append("; LayerHeight : " + p[0] + "\n");
    sb.Append("; Diameter of pillars : " + p[1] + "\n");
    sb.Append("; Num of Pillar : " + p[2] + "\n");
    sb.Append("; Pitch of pillars : " + p[3] + "\n");
    sb.Append("; Height of Pillars : " + p[4] + "\n");
    sb.Append("; Amount of material per 1mm : " + p[5] + "\n");
  }

  void WriteFile(String str, String path){
    StreamWriter sw = new StreamWriter(path, false, Encoding.GetEncoding("UTF-8"));
    sw.Write(str);
    sw.Close();
  }

  String makeRetraction(double amount, int speed, double sign){
    string mes = "";
    if(sign == -1){
      mes = " ; Retraction";
    }else{
      mes = " ; Extraction";
    }

    return "G1 F" + speed + " E" + (sign * amount) + mes + "\n";
  }

  String makeHairGcode(Vector3d fr, Vector3d to, double e, int f)
  {
    return "G1 F" + f + " X" + to.X.ToString("F2") + " Y" + to.Y.ToString("F2") + " E" + e.ToString("F6") + "\n";
  }


  String makeGcode(Vector3d fr, Vector3d to)
  {
    double len = (to - fr).Length;
    double numerator = (_NozzleWidth * len * _LayerHeight);
    double denominator = (1.75 / 2) * (1.75 / 2) * Math.PI;

    double e = numerator / denominator;

    return "G1 F1200 X" + to.X.ToString("F2") + " Y" + to.Y.ToString("F2") + " E" + e.ToString("F8") + "\n";
  }

  String makeGcode(Vector3d fr, Vector3d to, double speed)
  {
    double len = (to - fr).Length;
    double numerator = (_NozzleWidth * len * _LayerHeight);
    double denominator = (1.75 / 2) * (1.75 / 2) * Math.PI;

    double e = numerator / denominator;

    return "G1 F" + speed + " X" + to.X.ToString("F2") + " Y" + to.Y.ToString("F2") + " E" + e.ToString("F8") + "\n";
  }

  String makeGcode(Vector3d to){
    return "G0 F9000 X" + to.X.ToString("F2") + " Y" + to.Y.ToString("F2") + "\n";
  }
  // </Custom additional code> 
}
