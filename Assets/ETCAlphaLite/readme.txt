NOTE:使用导入shader请注意，是否覆盖了之前NGUI的shader
1、使用请打开Editor/ETCTexturePostprocessor.cs文件，修改 ADD TARGET FOLDER TO AUTO USE ETC + ALPHA 下面的文件目录自动处理信息
2、切换平台Android、iOS将自动对设置的目录进行贴图转换
3、在工具栏TextureSetting/ETC/可以手动转换（全部设置、选择目录转换）
4、在Editor/ETCTexturePostprocessor.cs的EditorSelectedFolderWindow中可以输入一些shader列表以供选择
5、手动目录转换窗口增加DontChanged选项将使用RGBA32真彩贴图


version 1.0
- FIX:自动设置窗口列表高度不对
- FIX:如果目录图片过多导致的内存暴涨问题