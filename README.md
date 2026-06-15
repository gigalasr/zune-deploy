# Zune Deploy
[![Publish App Image](https://github.com/gigalasr/zune-deploy/actions/workflows/app-image.yaml/badge.svg?branch=main)](https://github.com/gigalasr/zune-deploy/actions/workflows/app-image.yaml)
[![Run Tests](https://github.com/gigalasr/zune-deploy/actions/workflows/tests.yaml/badge.svg)](https://github.com/gigalasr/zune-deploy/actions/workflows/tests.yaml)

_This project is not affiliated with or endorsed by Microsoft_

<div align=center>

![logo](docs/logo-small.png)

CLI tool for deploying XNA applications to the Zune on Linux.

</div>


## Build
First install the necessary dependencies:
```shell
dotnet-sdk-10.0 build-essential cmake libssl-dev file
```

Then simply run:
```shell
dotnet build
```

## Usage
To deploy a Deploy Kit folder to a Zune you can use the `deploy` subcommand.
This will automatically deploy the XNA runtime and all files in the provided folder:

```
zcli deploy [path to folder]
```

---

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
