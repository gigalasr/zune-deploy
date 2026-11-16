# Zune Deploy
[![Publish App Image](https://github.com/gigalasr/zune-deploy/actions/workflows/app-image.yaml/badge.svg?branch=main)](https://github.com/gigalasr/zune-deploy/actions/workflows/app-image.yaml)
[![Run Tests](https://github.com/gigalasr/zune-deploy/actions/workflows/tests.yaml/badge.svg)](https://github.com/gigalasr/zune-deploy/actions/workflows/tests.yaml)

_This project is not affiliated with or endorsed by Microsoft_

<div align=center>

![logo](docs/logo-small.png)

CLI tool for deploying XNA applications to the Zune on Linux / MacOS / Windows.

</div>


Checkout [Project Lyra](https://github.com/project-lyra-zune/project-lyra/) for Zune HD modding. 

## Build
First install the necessary dependencies:
```shell
dotnet-sdk-10.0 build-essential cmake libssl-dev 
```

Clone this repository:
```shell
git clone --recurse-submodules https://github.com/gigalasr/zune-deploy 
```

Then simply run:
```shell
dotnet build
```

## Usage
First, make sure that a `.mtpz-data` file is located in your home directory (`%USERPROFILE%` on Windows).
You can find a copy of the `.mtpz-data` file in the `docs` folder.

To deploy a Deploy Kit folder to a Zune you can use the `deploy` subcommand.
This will automatically deploy the XNA runtime and all files in the provided folder:

```
zcli deploy [path to folder]
```

On Windows you need to first install WinUSB driver.
To simplify installation I would suggest using [Zadig](https://zadig.akeo.ie/).

---

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
