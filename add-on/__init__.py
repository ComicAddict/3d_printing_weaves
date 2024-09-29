bl_info = {
    "name": "g_code_exporter",
    "description": "G-code Importer/Editor/Re-Exporter",
    "author": "Tolga Yildiz",
    "version": (0, 0, 1),
    "blender": (4, 0, 0),
    "location": "3D View > g_code_exporter",
    "category": "Import-Export"
}



if "bpy" in locals():
    import importlib
    importlib.reload(utils) 
    importlib.reload(parser) 
    importlib.reload(gcode_export) 


else:
    from . import utils
    from . import parser
    from . import gcode_export
    
import bpy
from bpy.props import PointerProperty
    
    
    
    


def register():
    bpy.utils.register_class(gcode_export.EXPORTER_PT_Panel)
    bpy.utils.register_class(gcode_export.gcode_settings)
    bpy.utils.register_class(gcode_export.WM_OT_gcode_import)
    bpy.utils.register_class(gcode_export.WM_OT_gcode_export)
    bpy.types.Scene.gcode_export = bpy.props.PointerProperty(type= gcode_export.gcode_settings)
 



def unregister():
    bpy.utils.unregister_class(gcode_export.EXPORTER_PT_Panel)
    bpy.utils.unregister_class(gcode_export.gcode_settings)
    bpy.utils.unregister_class(gcode_export.WM_OT_gcode_import)
    bpy.utils.unregister_class(gcode_export.WM_OT_gcode_export)
    del bpy.types.Scene.gcode_export



if __name__ == "__main__":
    register()
