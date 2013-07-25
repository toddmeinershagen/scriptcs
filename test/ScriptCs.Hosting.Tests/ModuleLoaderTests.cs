﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Should;

namespace ScriptCs.Hosting.Tests
{
    public class ModuleLoaderTests
    {
        public class TheLoadMethod
        {
            private Mock<IAssemblyResolver> _mockAssemblyResolver = new Mock<IAssemblyResolver>();
            private IList<string> _paths = new List<string>();
            private IList<Lazy<IModule, IModuleMetadata>> _modules = new List<Lazy<IModule, IModuleMetadata>>();
            private Func<CompositionContainer, IEnumerable<Lazy<IModule, IModuleMetadata>>> _getModules;
            private Mock<IModule> _mockModule1 = new Mock<IModule>();
            private Mock<IModule> _mockModule2 = new Mock<IModule>();
            private Mock<IModule> _mockModule3 = new Mock<IModule>();

            public TheLoadMethod()
            {
                var paths = new [] {"path1", "path2"};
                _mockAssemblyResolver.Setup(r => r.GetAssemblyPaths(It.IsAny<string>())).Returns(paths);
                _modules.Add(new Lazy<IModule, IModuleMetadata>(()=>_mockModule1.Object, new ModuleMetadata {Extensions="ext1,ext2", Name="module1"}));
                _modules.Add(new Lazy<IModule, IModuleMetadata>(() => _mockModule2.Object, new ModuleMetadata { Extensions = "ext3,ext4", Name = "module2" }));
                _modules.Add(new Lazy<IModule, IModuleMetadata>(() => _mockModule3.Object, new ModuleMetadata { Name = "module3" }));
                _getModules = c => _modules;
                
            }

            [Fact]
            public void ShouldResolvePathsFromTheAssemblyResolver()
            {
                var loader = new ModuleLoader(_mockAssemblyResolver.Object, (p,c) => { }, c => Enumerable.Empty<Lazy<IModule,IModuleMetadata>>());
                loader.Load(null, "c:\test", null, false, false);
                _mockAssemblyResolver.Verify(r=>r.GetAssemblyPaths("c:\test"));
            }

            [Fact]
            public void ShouldInvokeTheCatalogActionForEachFile()
            {
                var loader = new ModuleLoader(_mockAssemblyResolver.Object, (p, c) => _paths.Add(p), c => Enumerable.Empty<Lazy<IModule, IModuleMetadata>>());
                loader.Load(null, "c:\test", null, false, false);
                _paths.Count.ShouldEqual(2);            
            }

            [Fact]
            public void ShouldInitializeModulesThatMatchOnExtension()
            {
                var loader = new ModuleLoader(_mockAssemblyResolver.Object, (p, c) => _paths.Add(p), _getModules);
                loader.Load(null, null, "ext1", false, false);
                _mockModule1.Verify(m=>m.Initialize(It.IsAny<IScriptRuntimeBuilder>(), false, false), Times.Once());
                _mockModule2.Verify(m => m.Initialize(It.IsAny<IScriptRuntimeBuilder>(), false, false), Times.Never());
                _mockModule3.Verify(m => m.Initialize(It.IsAny<IScriptRuntimeBuilder>(), false, false), Times.Never());

                loader = new ModuleLoader(_mockAssemblyResolver.Object, (p, c) => _paths.Add(p), _getModules);
                loader.Load(null, null, "ext3", false, false);
                _mockModule1.Verify(m => m.Initialize(It.IsAny<IScriptRuntimeBuilder>(), false, false), Times.Never());
                _mockModule2.Verify(m => m.Initialize(It.IsAny<IScriptRuntimeBuilder>(), false, false), Times.Once());
                _mockModule3.Verify(m => m.Initialize(It.IsAny<IScriptRuntimeBuilder>(), false, false), Times.Never());

            }

            [Fact]
            public void ShouldInitializeModulesTheMatchBasedOnName()
            {
                var loader = new ModuleLoader(_mockAssemblyResolver.Object, (p, c) => _paths.Add(p), _getModules);
                loader.Load(null, null, null, false, false, "module3");
                _mockModule1.Verify(m => m.Initialize(It.IsAny<IScriptRuntimeBuilder>(), false, false), Times.Never());
                _mockModule2.Verify(m => m.Initialize(It.IsAny<IScriptRuntimeBuilder>(), false, false), Times.Never());
                _mockModule3.Verify(m => m.Initialize(It.IsAny<IScriptRuntimeBuilder>(), false, false), Times.Once());
            }

            public class ModuleMetadata : IModuleMetadata
            {
                public string Name { get; set; }
                public string Extensions { get; set; }
            }
        }
    }
}
